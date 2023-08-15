﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    enum WebFrom
    {
        Baha = 0 , Anime = 1  , Myself = 2 , HAnime = 3 , Gimy = 4
    }

    class AnimeModel : INotifyPropertyChanged
    {
        public AnimeModel()
        {
            StartTime = DateTime.Now ;
            Quality = "720";
            _BarMax = 1;
            _Bar = 0;
        }

        public int No { set; get; }

        public WebFrom From { set; get; }
        #region 狀態

        Boolean _IsOk = false; //已完成下載
        public Boolean IsOk { get { return _IsOk; } set { _IsOk = value; TriggerUpdate("IsOk"); } }

        public Boolean IsIng { set; get; } = false; //正在下載

        Boolean _IsStop = false; // 出現錯誤被中斷
        public Boolean IsStop { get { return _IsStop; } set { _IsStop = value; TriggerUpdate("IsStop"); } }

        #endregion

        public String SN { set; get; }

        public String Span { set; get; }

        public String Url { set; get; }

        public String Name { set; get; }

        public DateTime StartTime { set; get; }

        public String DeviceId { set; get; }

        public String Quality { set; get; }

        public String Res { set; get; }

        public String Tmp { set; get; }

        String _Status;
        public String Status { get { return _Status; } set { _Status = value; TriggerUpdate("Status"); }  }

        Int32 _BarMax;
        public Int32 BarMax { get { return _BarMax; } set { _BarMax = value; TriggerUpdate("BarMax"); } }

        Int32 _Bar;
        public Int32 Bar { get { return _Bar; }set { _Bar = value; TriggerUpdate("Bar"); } }

        public List<String> ChuckList;

        public event PropertyChangedEventHandler PropertyChanged;
        internal void TriggerUpdate(String PropertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
            }
        }
    }
}
