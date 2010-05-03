﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SandRibbon.Providers;
using SandRibbon.Providers.Structure;
using SandRibbonInterop;
using SandRibbonObjects;

namespace SandRibbon.Utils.Connection
{
    public class Printer
    {
        private static int targetPageCount;
        public PrinterInformation PrinterInfo = new PrinterInformation();
        public class PrintParser : PreParser
        {
            public List<object> history = new List<object>();
            public PrintParser(int slide)
                : base(slide)
            {//This int constructor only passes to the superclass
            }
            public IEnumerable<InkCanvas> ToVisualWithNotes()
            {
                return createVisual();
            }
            public IEnumerable<InkCanvas> ToVisuaWithoutNotes()
            {
                var canvases = createVisual();
                return new [] {canvases.First()};
            }
            private IEnumerable<InkCanvas> createVisual()
            {
                var publicCanvas = new InkCanvas();
                var privateCanvas = new InkCanvas();
                foreach (var stroke in ink)
                {
                    if((stroke.privacy == "public" || stroke.target=="presentationSpace") && (stroke.target != "quizAuthoring"))
                        publicCanvas.Strokes.Add(stroke.stroke);
                    else if(stroke.target== "notepad")
                        privateCanvas.Strokes.Add(stroke.stroke);
                }
                foreach (var image in images)
                {
                    if ((image.Value.privacy == "public" || image.Value.target == "presentationSpace") && (image.Value.target != "quizAuthoring"))
                        publicCanvas.Children.Add(image.Value.image);
                    else if(image.Value.target== "notepad")
                        privateCanvas.Children.Add(image.Value.image);    
                }
                foreach (var box in text)
                {
                    var textbox = box.Value.box;
                    textbox.BorderThickness = new Thickness(0);
                    textbox.BorderBrush = new SolidColorBrush(Colors.Transparent);
                    textbox.Background = new SolidColorBrush(Colors.Transparent);
                    if ((box.Value.privacy == "public" || box.Value.target == "presentationSpace") && (box.Value.target != "quizAuthoring"))
                        publicCanvas.Children.Add(textbox);
                    else if(box.Value.target== "notepad")
                        privateCanvas.Children.Add(textbox);    
                }
                if (privateCanvas.Children.Count == 0 && privateCanvas.Strokes.Count == 0)
                    return new [] {publicCanvas};
                return new [] {publicCanvas, privateCanvas};
            }

            public override void actOnQuizReceived(QuizDetails quizDetails)
            {
                //Nothing.  Printer doesn't care about quiz
            }
        }
        public class PrinterInformation
        {
            public List<Slide> slides;
            public string title;
            public Dictionary<string, PrintParser> parsers = new Dictionary<string, PrintParser>();
        }
        public void PrintHandout(string title, string user)
        {
            var printDocument = new Action<IEnumerable<PrintParser>>(ShowPrintDialogWithoutNotes);
            var conversation = ConversationDetailsProviderFactory.Provider.DetailsOf(title);
            targetPageCount = conversation.Slides.Where(s=>s.type == Slide.TYPE.SLIDE).Count();
            PrinterInfo = new PrinterInformation
                              {
                                  parsers = new Dictionary<string, PrintParser>(),
                                  title = title,
                                  slides = conversation.Slides
                              };
            foreach (var slide in conversation.Slides.Where(s=>s.type==Slide.TYPE.SLIDE).OrderBy(s => s.index))
            {
                var room = slide.id;
                HistoryProviderFactory.provider.Retrieve<PrintParser>(
                                null,
                                null,
                                (parser)=> ReceiveParser(parser, printDocument),
                                room.ToString());
            }
        }
        public void PrintPrivate(string title, string user)
        {

            var printDocument = new Action<IEnumerable<PrintParser>>(ShowPrintDialogWithNotes);
            var conversation = ConversationDetailsProviderFactory.Provider.DetailsOf(title);
            targetPageCount = conversation.Slides.Where(s=>s.type == Slide.TYPE.SLIDE).Count();
            PrinterInfo = new PrinterInformation
                              {
                                  parsers = new Dictionary<string, PrintParser>(),
                                  title = title,
                                  slides = conversation.Slides
                              };
            foreach (var slide in conversation.Slides.Where(s=>s.type==Slide.TYPE.SLIDE).OrderBy(s => s.index))
            {
                var room = slide.id;
                var parsers = new List<PrintParser>();
                HistoryProviderFactory.provider.Retrieve<PrintParser>(
                                null,
                                null,
                                (parser)=>{
                                    parsers.Add(parser);
                                    if (parsers.Count() == 2)
                                        ReceiveParser(parsers[0].merge<PrintParser>(parsers[1]),printDocument );
                                },
                                room.ToString());
                HistoryProviderFactory.provider.RetrievePrivateContent<PrintParser>(
                                null,
                                null,
                                (parser) =>
                                {
                                    parsers.Add(parser);
                                    if (parsers.Count() == 2)
                                        ReceiveParser(parsers[0].merge<PrintParser>(parsers[1]), printDocument);
                                },
                                user,
                                room.ToString());
            }
        }
        private void ReceiveParser(PrintParser parser, Action<IEnumerable<PrintParser>> ShowPrintDialog)
        {
            PrinterInfo.parsers.Add(parser.location.currentSlide.ToString(), parser);
            if (PrinterInfo.parsers.Count() == targetPageCount)
            {
                var indicesByJid = PrinterInfo.slides.Aggregate(new Dictionary<string,int>(),
                    (acc,item)=>
                        {
                            acc.Add(item.id.ToString(), item.index);
                            return acc;
                        });
                ShowPrintDialog(from p in PrinterInfo.parsers orderby indicesByJid[p.Value.location.currentSlide.ToString()] select p.Value);
            }
        }
        private void ShowPrintDialogWithNotes(IEnumerable<PrintParser> parsers)
        {
            var visuals = parsers.Select(p => p.ToVisualWithNotes())
                                 .Aggregate(new List<InkCanvas>(),
                                                           (acc, item) => {
                                                                              acc.AddRange(item);
                                                                              return acc;
                                                           });
            HandlePrint(visuals);
        }
        private void ShowPrintDialogWithoutNotes(IEnumerable<PrintParser> parsers)
        {
            var visuals = parsers.Select(p => p.ToVisuaWithoutNotes())
                                 .Aggregate(new List<InkCanvas>(),
                                                           (acc, item) => {
                                                                              acc.AddRange(item);
                                                                              return acc;
                                                           });
            HandlePrint(visuals);
        }
        private void HandlePrint(List<InkCanvas> visuals)
        {
            Application.Current.Dispatcher.BeginInvoke((System.Action)delegate
              {
                  var printer = new PrintDialog { PageRangeSelection = PageRangeSelection.AllPages };
                  var result = printer.ShowDialog();
                  if ((bool)result)
                  {
                      var myDocument = new FixedDocument();
                      foreach (var visual in visuals)
                      {
                          var pageContent = new PageContent();
                          var page = new FixedPage
                                         {
                                             Width = printer.PrintableAreaWidth,
                                             Height = printer.PrintableAreaHeight
                                         };
                          var viewbox = new Viewbox();
                          viewbox.Width = page.Width;
                          viewbox.Height = page.Height;
                          viewbox.Child = (UIElement)visual;
                          page.Children.Add(viewbox);
                          ((IAddChild)pageContent).AddChild(page);
                          myDocument.Pages.Add(pageContent);
                      }
                      printer.PrintDocument(myDocument.DocumentPaginator, "A document");
                  }
              });
        }

        private class ThumbBox : Viewbox
        {
            public static int THUMBNAIL_WIDTH = 96;
            public static int THUMBNAIL_HEIGHT = 96;
            private string filename;
            public ThumbBox(UIElement child, string filename) : base()
            {
                this.Child = child;
                this.filename = filename;
                this.Width = THUMBNAIL_WIDTH;
                this.Height = THUMBNAIL_HEIGHT;
            }
            public void Save()
            {
                var descendantRect = VisualTreeHelper.GetDescendantBounds(this);
                this.UpdateLayout();
                var bitmap = new RenderTargetBitmap((int)Width, (int)Height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(this);
                BitmapFrame frame = BitmapFrame.Create(bitmap);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(frame);
                using (var stream = File.Create(filename))
                    encoder.Save(stream);
            }
        }
    }
}
