using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AniGamer.Module
{
    class SeasonInfo
    {
        public readonly String Id;
        public readonly String DisplayName;
        public readonly List<EpisodeInfo> Episodes;
        public SeasonInfo(String id, String displayName)
        {
            this.Id = id;
            this.DisplayName = displayName;
            this.Episodes = new List<EpisodeInfo>();
        }
    }
}
