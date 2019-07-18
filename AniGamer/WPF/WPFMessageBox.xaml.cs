using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop; 
using System.Runtime.InteropServices;
using System.Collections.Generic;

//直接放到主要命名空間裡  靜態呼叫可避免與System.Windows的MessageBox產生衝突
namespace Module
{
    
    /// <summary>
    /// WPFMessageBox.xaml 的互動邏輯
    /// </summary>
    public partial class WPFMessageBox : Window 
    {
        
        public WPFMessageBox()
        {
            InitializeComponent();
        }


        public WPFMessageBoxResult Result { get; set; }


        public static WPFMessageBoxResult Show(string 訊息)
        {
            return Show(訊息,  string.Empty, WPFMessageBoxButton.OK);
        }

        public static WPFMessageBoxResult Show(string 訊息, string details)
        {
            return Show(訊息,  details, WPFMessageBoxButton.OK);
        }

        public static WPFMessageBoxResult Show(string 訊息, WPFMessageBoxButton 按鍵樣式)
        {
            return Show(訊息, string.Empty, 按鍵樣式);
        }

        public static WPFMessageBoxResult Show(string 訊息,  string 詳細資訊, WPFMessageBoxButton 按鍵樣式)
        {
            _MessageBox = new WPFMessageBox();
            _MessageBox.Result = WPFMessageBoxResult.Close;
            if (詳細資訊 != "")
            {
                _MessageBox.Border.Height = 680;
                _MessageBox.Border.Width = 540;
            }
            if(Local.MainForm != null)
            Local.MainForm.Border_遮幕.Visibility = Visibility.Visible; //把主視窗變暗

            MessageBoxViewModel ViewModel = new MessageBoxViewModel(_MessageBox, 訊息, 詳細資訊, 按鍵樣式);
            _MessageBox.DataContext = ViewModel;
            try
            {
                _MessageBox.Owner = Local.MainForm;   //視窗綁在主視窗前
                _MessageBox.Border_背景.Background = new SolidColorBrush(Local.GetThemeColor("ImmersiveStartSelectionBackground"));
            }
            catch { }
            _MessageBox.ShowDialog();
            return _MessageBox.Result;
        }

        [ThreadStatic]
        static WPFMessageBox _MessageBox;


        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            { this.DragMove(); }
            catch
            { }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if(Local.MainForm != null)
            Local.MainForm.Border_遮幕.Visibility = Visibility.Collapsed;
        }
    }


    #region 自訂列舉

    public enum WPFMessageBoxButton
    {
        YesNo,
        YesNoCancel,
        OKCancel,
        OKClose,
        OK,
        Close
    }


    public enum WPFMessageBoxResult
    {
        Yes,
        No,
        Ok,
        Cancel,
        Close
    }

    public class DelegateCommand : ICommand
    {
        private readonly Action m_ExecuteMethod = null;
        private readonly Func<bool> m_CanExecuteMethod = null;
        private readonly bool m_IsAutomaticRequeryDisabled = false;
        private List<WeakReference> m_CanExecuteChangedHandlers;

        public DelegateCommand(Action executeMethod)
            : this(executeMethod, null, false) { }


        public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod, bool isAutomaticRequeryDisabled)
        {
            if (executeMethod == null) throw new ArgumentNullException("executeMethod");

            m_ExecuteMethod = executeMethod;
            m_CanExecuteMethod = canExecuteMethod;
            m_IsAutomaticRequeryDisabled = isAutomaticRequeryDisabled;
        }

        public bool CanExecute()
        {
            if (m_CanExecuteMethod != null) return m_CanExecuteMethod();
            return true;
        }

        public void Execute()
        {
            if (m_ExecuteMethod != null) m_ExecuteMethod();
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (!m_IsAutomaticRequeryDisabled) CommandManager.RequerySuggested += value;
                CommandManagerHelper.AddWeakReferenceHandler(ref m_CanExecuteChangedHandlers, value, 2);
            }
            remove
            {
                if (!m_IsAutomaticRequeryDisabled) CommandManager.RequerySuggested -= value;
                CommandManagerHelper.RemoveWeakReferenceHandler(m_CanExecuteChangedHandlers, value);
            }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        void ICommand.Execute(object parameter)
        {
            Execute();
        }
    }

    internal class CommandManagerHelper
    {
        internal static Action<List<WeakReference>> CallWeakReferenceHandlers = x =>
        {
            if (x != null)
            {

                var __Callers = new EventHandler[x.Count];
                int __Count = 0;

                for (int i = x.Count - 1; i >= 0; i--)
                {
                    var __Reference = x[i];
                    if (!(__Reference.Target is EventHandler __Handler))
                    {
                        x.RemoveAt(i);
                    }
                    else
                    {
                        __Callers[__Count] = __Handler;
                        __Count++;
                    }
                }
                for (int i = 0; i < __Count; i++)
                {
                    var __Handler = __Callers[i];
                    __Handler(null, EventArgs.Empty);
                }
            }
        };

        internal static Action<List<WeakReference>> AddHandlersToRequerySuggested = x =>
        {
            if (x != null)
            {
                x.ForEach(y =>
                {
                    if (y.Target is EventHandler __Handler) CommandManager.RequerySuggested += __Handler;
                });
            }
        };

        internal static Action<List<WeakReference>> RemoveHandlersFromRequerySuggested = x =>
        {
            if (x != null)
            {
                x.ForEach(y =>
                {
                    if (y.Target is EventHandler __Handler) CommandManager.RequerySuggested -= __Handler;
                });
            }
        };

        internal static void AddWeakReferenceHandler(ref List<WeakReference> handlers, EventHandler handler)
        {
            AddWeakReferenceHandler(ref handlers, handler, -1);
        }

        internal static void AddWeakReferenceHandler(ref List<WeakReference> handlers, EventHandler handler, int defaultListSize)
        {
            if (handlers == null)
            {
                handlers = (defaultListSize > 0 ? new List<WeakReference>(defaultListSize) : new List<WeakReference>());
            }

            handlers.Add(new WeakReference(handler));
        }

        internal static Action<List<WeakReference>, EventHandler> RemoveWeakReferenceHandler = (x, y) =>
        {
            if (x != null)
            {
                for (int i = x.Count - 1; i >= 0; i--)
                {
                    var __Reference = x[i];
                    if ((!(__Reference.Target is EventHandler __ExistingHandler)) || (__ExistingHandler == y))
                    {
                        x.RemoveAt(i);
                    }
                }
            }
        };
    }
    #endregion

    //MessageBox Binding Model
    public class MessageBoxViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Title
        {
            get { return _Title; }
            set
            {
                if (_Title != value)
                {
                    _Title = value;
                    NotifyPropertyChange("Title");
                }
            }
        }

        public string Message
        {
            get { return _Message; }
            set
            {
                if (_Message != value)
                {
                    _Message = value;
                    NotifyPropertyChange("Message");
                }
            }
        }

        public string InnerMessageDetails
        {
            get { return _InnerMessageDetails; }
            set
            {
                if (_InnerMessageDetails != value)
                {
                    _InnerMessageDetails = value;
                    NotifyPropertyChange("InnerMessageDetails");
                }
            }
        }


        public Visibility YesNoVisibility
        {
            get { return _YesNoVisibility; }
            set
            {
                if (_YesNoVisibility != value)
                {
                    _YesNoVisibility = value;
                    NotifyPropertyChange("YesNoVisibility");
                }
            }
        }

        public Visibility CancelVisibility
        {
            get { return _CancelVisibility; }
            set
            {
                if (_CancelVisibility != value)
                {
                    _CancelVisibility = value;
                    NotifyPropertyChange("CancelVisibility");
                }
            }
        }

        public Visibility OkVisibility
        {
            get { return _OKVisibility; }
            set
            {
                if (_OKVisibility != value)
                {
                    _OKVisibility = value;
                    NotifyPropertyChange("OkVisibility");
                }
            }
        }

        public Visibility CloseVisibility
        {
            get { return _CloseVisibility; }
            set
            {
                if (_CloseVisibility != value)
                {
                    _CloseVisibility = value;
                    NotifyPropertyChange("CloseVisibility");
                }
            }
        }

        public Visibility ShowDetails
        {
            get { return _ShowDetails; }
            set
            {
                if (_ShowDetails != value)
                {
                    _ShowDetails = value;
                    NotifyPropertyChange("ShowDetails");
                }
            }
        }

        public ICommand YesCommand
        {
            get
            {
                if (_YesCommand == null)
                    _YesCommand = new DelegateCommand(() =>
                    {
                        _View.Result = WPFMessageBoxResult.Yes;
                        _View.Close();
                    });
                return _YesCommand;
            }
        }

        public ICommand NoCommand
        {
            get
            {
                if (_NoCommand == null)
                    _NoCommand = new DelegateCommand(() =>
                    {
                        _View.Result = WPFMessageBoxResult.No;
                        _View.Close();
                    });
                return _NoCommand;
            }
        }

        public ICommand CancelCommand
        {
            get
            {
                if (_CancelCommand == null)
                    _CancelCommand = new DelegateCommand(() =>
                    {
                        _View.Result = WPFMessageBoxResult.Cancel;
                        _View.Close();
                    });
                return _CancelCommand;
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                if (_CloseCommand == null)
                    _CloseCommand = new DelegateCommand(() =>
                    {
                        _View.Result = WPFMessageBoxResult.Close;
                        _View.Close();
                    });
                return _CloseCommand;
            }
        }

        public ICommand OkCommand
        {
            get
            {
                if (_OKCommand == null)
                    _OKCommand = new DelegateCommand(() =>
                    {
                        _View.Result = WPFMessageBoxResult.Ok;
                        _View.Close();
                    });
                return _OKCommand;
            }
        }

        public MessageBoxViewModel(WPFMessageBox view, string message, string innerMessage, WPFMessageBoxButton buttonOption)
        {
            Message = message;
            InnerMessageDetails = innerMessage;
            SetButtonVisibility(buttonOption);
            _View = view;
            _View.KeyDown += _View_KeyDown;
        }

        private void NotifyPropertyChange(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        private void SetButtonVisibility(WPFMessageBoxButton buttonOption)
        {
            switch (buttonOption)
            {
                case WPFMessageBoxButton.YesNo:
                    OkVisibility = CancelVisibility = CloseVisibility = Visibility.Collapsed;
                    break;
                case WPFMessageBoxButton.YesNoCancel:
                    OkVisibility = CloseVisibility = Visibility.Collapsed;
                    break;
                case WPFMessageBoxButton.OK:
                    YesNoVisibility = CancelVisibility = CloseVisibility = Visibility.Collapsed;
                    break;
                case WPFMessageBoxButton.OKClose:
                    YesNoVisibility = CancelVisibility = Visibility.Collapsed;
                    break;
                default:
                    OkVisibility = CancelVisibility = YesNoVisibility = Visibility.Collapsed;
                    break;
            }
            if (string.IsNullOrEmpty(InnerMessageDetails)) ShowDetails = Visibility.Collapsed;
            else ShowDetails = Visibility.Visible;
        }

        private void _View_KeyDown(object sender, KeyEventArgs e)
        {
           switch(e.Key)
            {
                case Key.Y: if (_YesNoVisibility == Visibility.Visible) YesCommand.Execute(null); break;
                case Key.N: if (_YesNoVisibility == Visibility.Visible) NoCommand.Execute(null); break;
                case Key.O: if (_OKVisibility == Visibility.Visible) OkCommand.Execute(null); break;
                case Key.C: if (_CancelVisibility == Visibility.Visible) CancelCommand.Execute(null); break;
            }
        }


        private string _Title;
        private string _Message;
        private string _InnerMessageDetails;

        private Visibility _YesNoVisibility;
        private Visibility _CancelVisibility;
        private Visibility _OKVisibility;
        private Visibility _CloseVisibility;
        private Visibility _ShowDetails;

        private ICommand _YesCommand;
        private ICommand _NoCommand;
        private ICommand _CancelCommand;
        private ICommand _CloseCommand;
        private ICommand _OKCommand;

        private WPFMessageBox _View;
    }

}

