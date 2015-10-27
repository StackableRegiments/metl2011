﻿using MeTLLib;
using MeTLLib.DataTypes;
using MeTLLib.Providers.Connection;
using SandRibbon.Components;
using SandRibbon.Pages.Collaboration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SandRibbon.Pages.Conversations.Models
{
    public class Participation : DependencyObject
    {
        public ObservableCollection<LocatedActivity> Activity
        {
            get { return (ObservableCollection<LocatedActivity>)GetValue(ActivityProperty); }
            set { SetValue(ActivityProperty, value); }
        }
        public static readonly DependencyProperty ActivityProperty =
            DependencyProperty.Register("Activity", typeof(ObservableCollection<LocatedActivity>), typeof(Participation), new PropertyMetadata(new ObservableCollection<LocatedActivity>()));

        public ILookup<string, LocatedActivity> Participants { get; internal set; }
    }
    public class VmSlide : DependencyObject
    {
        public ConversationRelevance Relevance { get; set; }
        public Slide Slide { get; set; }
        public ConversationDetails Details { get; set; }

        public Participation Participation
        {
            get { return (Participation)GetValue(ParticipationProperty); }
            set { SetValue(ParticipationProperty, value); }
        }
        public static readonly DependencyProperty ParticipationProperty =
            DependencyProperty.Register("Participation", typeof(Participation), typeof(VmSlide), new PropertyMetadata(null));
    }
    public enum ConversationRelevance
    {
        PRESENTATION_PATH,
        ADVANCED_MATERIAL,
        REMEDIAL_MATERIAL,
        RELATED_MATERIAL
    }
    public class ReticulatedConversation : DependencyObject
    {
        public ConversationDetails PresentationPath { get; set; }
        public ConversationDetails AdvancedMaterial { get; set; }
        public ConversationDetails RemedialMaterial { get; set; }
        public List<ConversationDetails> RelatedMaterial { get; set; } = new List<ConversationDetails>();
        public List<LearningObjective> Objectives { get; set; } = new List<LearningObjective>();
        private List<ConversationDetails> cds()
        {
            return new[]
                {
                PresentationPath,
                    AdvancedMaterial,
                    RemedialMaterial
                }.Concat(RelatedMaterial ?? new List<ConversationDetails>()).ToList();
        }
        public ObservableCollection<VmSlide> Locations { get; set; } = new ObservableCollection<VmSlide>();


        public ObservableCollection<LocatedActivity> Participation
        {
            get { return (ObservableCollection<LocatedActivity>)GetValue(ParticipationProperty); }
            set { SetValue(ParticipationProperty, value); }
        }
        public static readonly DependencyProperty ParticipationProperty =
            DependencyProperty.Register("Participation", typeof(ObservableCollection<LocatedActivity>), typeof(ReticulatedConversation), new PropertyMetadata(new ObservableCollection<LocatedActivity>()));


        public int LongestPathLength => cds().Where(cd => cd != null).Select(d => d.Slides.Count()).Max();
        public int PathCount => cds().Count();

        public delegate void LocationAnalysis();
        public event LocationAnalysis LocationAnalyzed;

        public void CalculateLocations()
        {
            var locs = new List<VmSlide>();
            PresentationPath?.Slides.ForEach(s => locs.Add(new VmSlide { Details = PresentationPath, Slide = s, Relevance = ConversationRelevance.PRESENTATION_PATH }));
            AdvancedMaterial?.Slides.ForEach(s => locs.Add(new VmSlide { Details = AdvancedMaterial, Slide = s, Relevance = ConversationRelevance.ADVANCED_MATERIAL }));
            RemedialMaterial?.Slides.ForEach(s => locs.Add(new VmSlide { Details = RemedialMaterial, Slide = s, Relevance = ConversationRelevance.REMEDIAL_MATERIAL }));
            for (int row = 0; row < RelatedMaterial.Count(); row++)
            {
                var details = RelatedMaterial[row];
                details?.Slides.ForEach(s => locs.Add(new VmSlide { Details = details, Slide = s, Relevance = ConversationRelevance.RELATED_MATERIAL }));
            }
            Locations.Clear();
            foreach (var loc in locs)
            {
                Locations.Add(loc);
            }
        }

        public void AnalyzeLocations()
        {
            foreach (var slide in Locations)
            {
                ClientFactory.Connection().getHistoryProvider().Retrieve<PreParser>(
                                    null,
                                    null,
                                    (parser) =>
                                    {
                                        LocationAnalyzed?.Invoke();
                                        foreach (var user in process(parser))
                                        {
                                            user.index = slide.Slide.index;
                                            Participation.Add(user);
                                            var localParticipation = Participation.Where(p => p.slide == slide.Slide.id);
                                            slide.Participation = new Participation
                                            {
                                                Participants = localParticipation.ToLookup(p => p.username),
                                                Activity = new ObservableCollection<LocatedActivity>(localParticipation)
                                            };
                                        }
                                    },
                                    slide.Slide.id.ToString());
            }
        }

        private void inc(Dictionary<string, int> dict, string author)
        {
            if (!dict.ContainsKey(author))
            {
                dict[author] = 1;
            }
            else
            {
                dict[author]++;
            }
        }

        private IEnumerable<LocatedActivity> process(PreParser p)
        {
            var tallies = new Dictionary<string, int>();
            foreach (var s in p.ink)
            {
                inc(tallies, s.author);
            }
            foreach (var t in p.text.Values)
            {
                inc(tallies, t.author);
            }
            foreach (var i in p.images.Values)
            {
                inc(tallies, i.author);
            }
            return tallies.Select(kv => new LocatedActivity(kv.Key, p.location.currentSlide, kv.Value, 0));
        }
    }

    public class ReticulatedNode
    {
        public List<ReticulatedNode> Outputs { get; set; }
        public SlideRestriction Restriction { get; set; }
        public string Narration { get; set; }
        public Slide Slide { get; set; }

        public ReticulatedNode()
        {
            Outputs = new List<ReticulatedNode>();
        }
    }

    public class SlideRestriction
    {
        public static SlideRestriction Unrestricted = new SlideRestriction { predicate = p => true };
        public Func<MeTLUser, bool> predicate { get; set; }
    }
}