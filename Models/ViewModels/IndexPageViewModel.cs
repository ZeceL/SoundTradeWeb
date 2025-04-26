using System.Collections.Generic;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class IndexPageViewModel
    {
        public IEnumerable<TrackIndexViewModel> RecommendedTracks { get; set; }
        public IEnumerable<TrackIndexViewModel> PopTracks { get; set; }
        public IEnumerable<TrackIndexViewModel> RockTracks { get; set; }
        public IEnumerable<TrackIndexViewModel> HipHopTracks { get; set; }
        public IEnumerable<TrackIndexViewModel> JazzTracks { get; set; }
        public IEnumerable<TrackIndexViewModel> ElectronicTracks { get; set; }
    }
}