﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using Divelements.SandRibbon;
using MeTLLib.DataTypes;
using MeTLLib.Providers.Connection;
using Microsoft.Practices.Composite.Presentation.Commands;
using Microsoft.Win32;
using SandRibbon.Providers;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using SandRibbon.Components.Utility;
using System.Collections.Generic;
using MeTLLib.Providers;

namespace SandRibbon.Tabs
{
    public class FileInfo
    {
        public string fileType { get; set; }
        public string filename { get; set; }
        public string url { get; set; }
        public string author {get; set;}
        public string fileImage { get; set; }
        public string uploadTime { get; set; }
        public long size { get; set; }
        public string tooltip { get; set; }
    }
    public partial class Attachments :RibbonTab 
    {
        private ObservableCollection<FileInfo> files; 
        public Attachments()
        {
            InitializeComponent();
            files = new ObservableCollection<FileInfo>();
            attachments.ItemsSource = files;
            Commands.ReceiveFileResource.RegisterCommand(new DelegateCommand<MeTLLib.DataTypes.TargettedFile>(receiveFile));
            Commands.PreParserAvailable.RegisterCommand(new DelegateCommand<PreParser>(preparserAvailable));
            Commands.JoinConversation.RegisterCommandToDispatcher(new DelegateCommand<object>(clearOutAttachments));
            Commands.UpdateConversationDetails.RegisterCommandToDispatcher(new DelegateCommand<ConversationDetails>(UpdateConversationDetails));
            Commands.FileUpload.RegisterCommand(new DelegateCommand<object>(uploadFile));
        }
        private void UpdateConversationDetails(ConversationDetails details)
        {
            if (details.IsEmpty) return;
            if (details.IsJidEqual(Globals.location.activeConversation) && details.isDeleted)
                clearOutAttachments(null);
        }
        private void clearOutAttachments(object obj)
        {
            files = new ObservableCollection<FileInfo>();
            attachments.ItemsSource = files;
        }

        private void preparserAvailable(PreParser preParser)
        {
            foreach(var file in preParser.files)
                receiveFile(file);
        }
        private void receiveFile(MeTLLib.DataTypes.TargettedFile fileInfo)
        {
            if(Globals.conversationDetails.Jid != fileInfo.conversationJid.ToString()) return;
            Dispatcher.adoptAsync(() => files.Add(new FileInfo
                                                     {
                                                         fileType = getFileType(fileInfo.name),
                                                         filename = fileInfo.name,
                                                         url = fileInfo.url,
                                                         author = fileInfo.author,
                                                         fileImage = getFileImage(fileInfo.name),
                                                         uploadTime =fileInfo.uploadTime, 
                                                         size = fileInfo.size,
                                                         tooltip = string.Format("Type: {0}\nAuthor: {1}\nUpload Time: {2}\nSize: {3:0.00}mb", getFileType(fileInfo.name), fileInfo.author, new DateTime(long.Parse(fileInfo.uploadTime)), fileInfo.size / 1048576.0) 
                                                     }));
        }
        private void saveFile(object sender, RoutedEventArgs e)
        {
            var file = (FileInfo)((Button) sender).DataContext;
            var saveFile = new SaveFileDialog();
            saveFile.FileName = file.filename;
            saveFile.Filter = string.Format("{0} (*{1})|*{1}|All Files (*.*)|*.*", file.fileType, System.IO.Path.GetExtension(file.filename));
            saveFile.FilterIndex = 1;
            saveFile.RestoreDirectory = true;
            if(saveFile.ShowDialog(Window.GetWindow(this)) == true)
            {
                var backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += (s, a) =>
                                               {
                                                   var stream = saveFile.OpenFile();
                                                   var sourceBytes = new WebClient { Credentials = new NetworkCredential("exampleUsername", "examplePassword") }.DownloadData(file.url);
                                                   stream.Write(sourceBytes, 0, sourceBytes.Count());
                                                   stream.Close();
                                               

                                               };
                backgroundWorker.RunWorkerCompleted += (s, a) => Dispatcher.Invoke(DispatcherPriority.Send,
                                                                                   (Action)(() => MeTLMessage.Information(string.Format("Finished downloading {0}.", saveFile.FileName))));
                backgroundWorker.RunWorkerAsync();
            }
        }
        //utility methods
        public static string getFileType(string url)
        {
            var extension = System.IO.Path.GetExtension(url).ToLower();
            switch (extension)
            {
                case ".ppt":
                    return "PowerPoint";
                case ".pptx":
                    return "PowerPoint";
                case ".doc":
                    return "Word";
                case ".docx":
                    return "Word";
                case ".txt":
                    return "Text";
                case ".html":
                    return "HTML";
                case ".xls":
                    return "Excel";
                case ".xlsx":
                    return "Excel";
                case ".pdf":
                    return "PDF";
                case ".odt":
                    return "Open Office Document";
                case ".mpg":
                    return "Video";
                case ".mp4":
                    return "Video";
                case ".m4v":
                    return "Video";
                case ".mpz":
                    return "Video";
                case ".mpeg":
                    return "Video";
                case ".divx":
                    return "Video";
                case ".xvid":
                    return "Video";
                case ".avi":
                    return "Video";
                case ".mov":
                    return "QuickTime";
                case ".swf":
                    return "Shockwave";
                case ".wmv":
                    return "Windows Media Video";
                case ".xap":
                    return "Silverlight";
                case ".gif":
                    return "GIF";
                case ".png":
                    return "PNG";
                case ".bmp":
                    return "Bitmap";
                case ".jpeg":
                    return "Jpeg";
                case ".jpg":
                    return "Jpeg";
                case ".mp3":
                    return "Audio";
                case ".wav":
                    return "Audio";
                default :
                    return "Other";
            }
        }
        public static string getFileImage(string url)
        {
            switch (getFileType(url))
            {
                case "HTML":
                    return "\\resources\\mimeTypes\\web.png";
                case "Jpeg":
                    return "\\resources\\mimeTypes\\image.png";
                case "Audio":
                    return "\\resources\\mimeTypes\\audio.png";
                case "Other":
                    return "\\resources\\mimeTypes\\unknown.png";
                case "Bitmap":
                    return "\\resources\\mimeTypes\\image.png";
                case "PDF":
                    return "\\resources\\mimeTypes\\publication.png";
                case "Text":
                    return "\\resources\\mimeTypes\\text.png";
                case "Word":
                    return "\\resources\\mimeTypes\\document.png";
                case "PowerPoint":
                    return "\\resources\\mimeTypes\\publication.png";
                case "Excel":
                    return "\\resources\\mimeTypes\\spreadsheet.png";
                case "PNG":
                    return "\\resources\\mimeTypes\\image.png";
                case "JPG":
                    return "\\resources\\mimeTypes\\image.png";
                case "GIF":
                    return "\\resources\\mimeTypes\\image.png";
                case "Windows Media Video":
                    return "\\resources\\mimeTypes\\video.png";
                case "Open Office Document":
                    return "\\resources\\mimeTypes\\document.png";
                case "Silverlight":
                    return "\\resources\\mimeTypes\\gadget.png";
                case "Shockwave":
                    return "\\resources\\mimeTypes\\gadget.png";
                case "Quicktime":
                    return "\\resources\\mimeTypes\\video.png";
                case "Video":
                    return "\\resources\\mimeTypes\\video.png";
                default:
                    return "\\resources\\mimeTypes\\unknown.png";
            }
        }
        private void uploadFile(object _obj)
        {
            addResourceFromDisk("All files (*.*)|*.*", (files) =>
                                    {
                                        foreach (var file in files)
                                        {
                                            uploadFileForUse(file);
                                        }
                                    });
        }
      
        private void addResourceFromDisk(Action<IEnumerable<string>> withResources)
        {
            const string filter = "Image files(*.jpeg;*.gif;*.bmp;*.jpg;*.png)|*.jpeg;*.gif;*.bmp;*.jpg;*.png|All files (*.*)|*.*";
            addResourceFromDisk(filter, withResources);
        }

        private void addResourceFromDisk(string filter, Action<IEnumerable<string>> withResources)
        {
            {
                string initialDirectory = "c:\\";
                foreach (var path in new[] { Environment.SpecialFolder.MyPictures, Environment.SpecialFolder.MyDocuments, Environment.SpecialFolder.DesktopDirectory, Environment.SpecialFolder.MyComputer })
                    try
                    {
                        initialDirectory = Environment.GetFolderPath(path);
                        break;
                    }
                    catch (Exception)
                    {
                    }
                var fileBrowser = new OpenFileDialog
                                             {
                                                 InitialDirectory = initialDirectory,
                                                 Filter = filter,
                                                 FilterIndex = 1,
                                                 RestoreDirectory = true
                                             };
                //DisableDragDrop(); 
                var dialogResult = fileBrowser.ShowDialog(Window.GetWindow(this));
                //EnableDragDrop();

                if (dialogResult ?? false)
                    withResources(fileBrowser.FileNames);
            }
        }

        private const int KILOBYTE = 1024;
        private const int MEGABYTE = 1024 * KILOBYTE;
        private bool isFileLessThanXMB(string filename, int size)
        {
            if (filename.StartsWith("http")) return true;
            var info = new System.IO.FileInfo(filename);
            if (info.Length > size * MEGABYTE)
            {
                return false;
            }
            return true;
        }
        private int fileSizeLimit = 50;
        private void uploadFileForUse(string unMangledFilename)
        {
            string filename = unMangledFilename + ".MeTLFileUpload";
            if (filename.Length > 260)
            {
                MeTLMessage.Information("Sorry, your filename is too long, must be less than 260 characters");
                return;
            }
            if (isFileLessThanXMB(unMangledFilename, fileSizeLimit))
            {
                var worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                 {
                     var target = "presentationSpace"; // looks like this can be "presentationSpace" or "notepad"
                     System.IO.File.Copy(unMangledFilename, filename);
                     MeTLLib.ClientFactory.Connection().UploadAndSendFile(
                         new MeTLStanzas.LocalFileInformation(Globals.slide, Globals.me, target, "public", filename, System.IO.Path.GetFileNameWithoutExtension(filename), false, new System.IO.FileInfo(filename).Length, SandRibbonObjects.DateTimeFactory.Now().Ticks.ToString()));
                     System.IO.File.Delete(filename);
                 };
                worker.RunWorkerCompleted += (s, a) => Dispatcher.Invoke(DispatcherPriority.Send,
                                                                                   (Action)(() => MeTLMessage.Information(string.Format("Finished uploading {0}.", unMangledFilename))));
                worker.RunWorkerAsync();
            }
            else
            {
                MeTLMessage.Information(String.Format("Sorry, your file is too large, must be less than {0}mb", fileSizeLimit));
                return;
            }
        }
        /*private void DisableDragDrop()
        {
            DragOver -= ImageDragOver;
            Drop -= ImagesDrop;
            DragOver += ImageDragOverCancel;
        }*/

        /*private void EnableDragDrop()
        {
            DragOver -= ImageDragOverCancel;
            DragOver += ImageDragOver;
            Drop += ImagesDrop;
        }*/
    }
}
