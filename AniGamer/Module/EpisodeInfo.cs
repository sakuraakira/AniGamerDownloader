using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AniGamer.Module
{
    class EpisodeInfo
    {
        public readonly String DisplayName;
        public readonly String Id;
        public EpisodeInfo(String id, String displayName)
        {
            this.Id = id;
            this.DisplayName = displayName;
        }
    }
}
