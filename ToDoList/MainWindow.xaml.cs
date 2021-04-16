using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ToDoList
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// TCP клиент для подключения к серверу
        /// </summary>
        private TcpClient tcpClient;
        /// <summary>
        /// Логин пользователя
        /// </summary>
        private string Login;
        /// <summary>
        /// Пароль пользователя
        /// </summary>
        private string Password;
        /// <summary>
        /// Таймер для сохранения
        /// </summary>
        Timer timer;
        /// <summary>
        /// Произойдет ли сохранение через определенное время
        /// </summary>
        private bool IsGoingToSave = false;
        /// <summary>
        /// Изменен ли текст
        /// </summary>
        private bool IsTextChanged = false;
        /// <summary>
        /// Дополнительная переменная, что б не менять IsTextChanged в поределенный момент.
        /// </summary>
        private bool IsTextCurrentlyChanged = false;
        /// <summary>
        /// ID выбранной группы
        /// </summary>
        private int SelectedGroupId = -1;
        /// <summary>
        /// ID выбранной заметки
        /// </summary>
        private int SelectedNoteId = -1;
        /// <summary>
        /// Расширено ли окно
        /// </summary>
        private bool IsSmall = true;
        /// <summary>
        /// Положение левой стороны окна перед расширением
        /// </summary>
        private double LastLeft;
        /// <summary>
        /// Положение верхней стороны окна перед расширением
        /// </summary>
        private double LastTop;
        /// <summary>
        /// IP сервера для подлючения
        /// </summary>
        private string ServerIp = "127.0.0.1";
        /// <summary>
        /// Порт сервера для подключения
        /// </summary>
        private int ServerPort = 1024;

        /// <summary>
        /// Коллекция групп с заметками
        /// </summary>
        List<Classes.Group> Groups;

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SelectedGroupId != -1)
            {
                SaveGroup(SelectedGroupId);
            }
        }


        //События
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void HideBut_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void ExpandBut_Click(object sender, RoutedEventArgs e)
        {
            if (IsSmall)
            {
                LastLeft = this.Left;
                LastTop = this.Top;
                this.Left = 0;
                this.Top = 0;
                this.Height = SystemParameters.WorkArea.Height;
                this.Width = SystemParameters.WorkArea.Width;
                outsideRectangle.Rect = new Rect(0, 0, SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height);
                IsSmall = false;
            }
            else
            {
                this.Left = LastLeft;
                this.Top = LastTop;
                this.Height = 450;
                this.Width = 800;
                outsideRectangle.Rect = new Rect(0, 0, 800, 450);
                IsSmall = true;
            }
        }
        private void CloseBut_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void LogInBut_Click(object sender, RoutedEventArgs e)
        {
            LogInMethod();
        }
        private void SignUpBut_Click(object sender, RoutedEventArgs e)
        {
            RegisterMethod();
        }
        private void IsPassVisible_Checked(object sender, RoutedEventArgs e)
        {
            PasswordTextBox.Text = PasswordBoxBox.Password;
            PasswordTextBox.Visibility = Visibility.Visible;
            PasswordBoxBox.Visibility = Visibility.Hidden;
        }
        private void IsPassVisible_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordBoxBox.Password = PasswordTextBox.Text;
            PasswordBoxBox.Visibility = Visibility.Visible;
            PasswordTextBox.Visibility = Visibility.Hidden;
        }
        //Группы
        private void AddGroupBut_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            CreateGroup();
        }
        private void GroupElement_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            (sender as TextBox).Cursor = Cursors.IBeam;
            (sender as TextBox).IsReadOnly = false;
            (sender as TextBox).SelectAll();
        }
        private void GroupElement_LostFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).Cursor = Cursors.Arrow;
            (sender as TextBox).IsReadOnly = true;
        }
        private void GroupElement_GotMouseCapture(object sender, MouseEventArgs e)
        {
            NoteMenuBorder.Visibility = Visibility.Visible;
            NoteMenu.Visibility = Visibility.Hidden;
            foreach (var item in GroupsPanel.Children)
            {
                (item as TextBox).Foreground = new SolidColorBrush(Colors.White);
                (item as TextBox).Background = new SolidColorBrush(Colors.Transparent);
            }

            e.Handled = false;
            (sender as TextBox).Background = new SolidColorBrush(Colors.White);
            (sender as TextBox).Foreground = new SolidColorBrush(Color.FromRgb(0, 52, 89));
            try
            {
                if (IsTextChanged)
                {
                    IsGoingToSave = true;
                    timer = new Timer(SaveGroup, SelectedGroupId, 500, Timeout.Infinite);
                }
                SelectedGroupId = Int32.Parse((sender as TextBox).Name.Substring(1));
                OpenNotesMenu();
            }
            catch (Exception)
            {
                ShowErrorWindow("Something went wrong");
            }

        }
        private void GroupElement_MouseEnter(object sender, MouseEventArgs e)
        {
            if ((sender as TextBox).Name != "g" + SelectedGroupId)
            {
                (sender as TextBox).Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
            }
        }
        private void GroupElement_MouseLeave(object sender, MouseEventArgs e)
        {
            if ((sender as TextBox).Name != "g" + SelectedGroupId)
            {
                (sender as TextBox).Background = new SolidColorBrush(Colors.Transparent);
            }
        }
        private void GroupElement_TextChanged(object sender, TextChangedEventArgs e)
        {
            int groupId = Int32.Parse((sender as TextBox).Name.Substring(1));
            Groups.Where(g => g.Id == groupId).FirstOrDefault().Title = (sender as TextBox).Text;
            IsTextChanged = true;
        }
        private void DelGroupBut_Click(object sender, RoutedEventArgs e)
        {
            DeleteGroup(SelectedGroupId);
        }
        //Заметки
        private void NoteElement_Click(object sender, RoutedEventArgs e)
        {
            SelectedNoteId = Int32.Parse((sender as CheckBox).Name.Substring(1));
            bool isChecked = false;
            if ((sender as CheckBox).IsChecked != null)
                isChecked = (bool)(sender as CheckBox).IsChecked;

            Groups.Where(g => g.Id == SelectedGroupId).FirstOrDefault().Notes.Where(n => n.Id == SelectedNoteId).FirstOrDefault().IsChecked = isChecked;
            IsTextChanged = true;
            ShowNoteDescription();
            SaveNote();
        }
        private void NoteElement_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectedNoteId = Int32.Parse((sender as CheckBox).Name.Substring(1));
            ShowNoteDescription();
        }
        private void AddNoteBut_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedGroupId != -1)
            {
                e.Handled = true;
                CreateNote();
            }
            else
            {
                ShowErrorWindow("Group is not selected");
            }
        }
        private void DelNoteBut_Click(object sender, RoutedEventArgs e)
        {
            DeleteNote(SelectedNoteId);
        }
        private void NoteBox_LostMouseCapture(object sender, MouseEventArgs e)
        {
            SaveNote();
        }
        private void NoteNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Groups.Where(g => g.Id == SelectedGroupId).FirstOrDefault().Notes.Where(n => n.Id == SelectedNoteId).FirstOrDefault().Title = NoteNameBox.Text;
            if (!IsTextCurrentlyChanged)
            {
                foreach (UIElement item in NotesPanel.Children)
                {
                    if ((item as CheckBox).Name == "n" + SelectedNoteId)
                    {
                        (item as CheckBox).Content = NoteNameBox.Text;
                    }
                }
                IsTextChanged = true;
            }
        }
        private void NoteDescriptionBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsTextCurrentlyChanged)
            {
                Groups.Where(g => g.Id == SelectedGroupId).FirstOrDefault().Notes.Where(n => n.Id == SelectedNoteId).FirstOrDefault().Description = NoteDescriptionBox.Text;
                IsTextChanged = true;
            }
        }


        //Работа с потоком
        private string ReadStringFromStream(NetworkStream stream)
        {
            byte[] buf = new byte[1024];
            int len = 0, sum = 0;
            List<byte> allBytes = new List<byte>();
            do
            {
                len = stream.Read(buf, 0, buf.Length);
                allBytes.AddRange(buf);
                sum += len;
            } while (len >= buf.Length);
            return Encoding.Unicode.GetString(allBytes.ToArray(), 0, sum);
        }
        private void WriteToStream(NetworkStream stream, string message)
        {
            byte[] buf = Encoding.Unicode.GetBytes(message);
            stream.Write(buf, 0, buf.Length);
        }


        //Вход пользователя
        private void LogInMethod(/*object obj*/)
        {
            ThicknessAnimation errorAnimation = new ThicknessAnimation();
            errorAnimation.Duration = TimeSpan.FromSeconds(0.4);
            errorAnimation.From = new Thickness(0);
            errorAnimation.To = new Thickness(3);
            errorAnimation.AccelerationRatio = 0.3;
            errorAnimation.DecelerationRatio = 0.7;
            errorAnimation.AutoReverse = true;

            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(ServerIp, ServerPort);
                string pass = (PasswordTextBox.Visibility == Visibility.Visible) ? PasswordTextBox.Text : PasswordBoxBox.Password;
                WriteToStream(tcpClient.GetStream(), $"LGIN {LoginBox.Text} {pass}");

                string data = ReadStringFromStream(tcpClient.GetStream());
                switch (data)
                {
                    case "OK": //Все правильно
                        {
                            Login = LoginBox.Text;
                            Password = PasswordTextBox.Text;

                            LoadGroups();
                            ShowSuccessWindow("Successfully logged in");
                            var animation = new ThicknessAnimation();
                            animation.From = new Thickness(0, 0, 0, 0);
                            animation.To = new Thickness(0, -500, 0, 500);
                            animation.Duration = TimeSpan.FromSeconds(1);
                            animation.AccelerationRatio = 0.3;
                            animation.DecelerationRatio = 0.7;
                            LogInGrid.BeginAnimation(MarginProperty, animation);
                            break;
                        }
                    case "PASS": //Неверный пароль
                        {
                            if (PasswordTextBox.Visibility == Visibility.Visible)
                                PasswordTextBox.BeginAnimation(BorderThicknessProperty, errorAnimation);
                            else
                                PasswordBoxBox.BeginAnimation(BorderThicknessProperty, errorAnimation);
                            break;
                        }
                    case "LOGIN": //Не найден логин
                        {
                            LoginBox.BeginAnimation(BorderThicknessProperty, errorAnimation);
                            break;
                        }
                    default:
                        MessageBox.Show("Error with answer.");
                        break;
                }
                tcpClient.Close();
            }
            catch (Exception)
            {
                ShowErrorWindow("Something went wrong");
            }
        }
        private void RegisterMethod(/*object obj*/)
        {
            ThicknessAnimation errorAnimation = new ThicknessAnimation();
            errorAnimation.Duration = TimeSpan.FromSeconds(0.4);
            errorAnimation.From = new Thickness(0);
            errorAnimation.To = new Thickness(3);
            errorAnimation.AccelerationRatio = 0.3;
            errorAnimation.DecelerationRatio = 0.7;
            errorAnimation.AutoReverse = true;

            string pass = (PasswordTextBox.Visibility == Visibility.Visible) ? PasswordTextBox.Text : PasswordBoxBox.Password;
            if (LoginBox.Text.Contains(' '))
            {
                LoginBox.BeginAnimation(BorderThicknessProperty, errorAnimation);
                ShowErrorWindow("Can't use spaces");
            }
            else if (pass == "")
            {
                PasswordTextBox.BeginAnimation(BorderThicknessProperty, errorAnimation);
            }
            else
            {
                try
                {
                    tcpClient = new TcpClient();
                    tcpClient.Connect(ServerIp, ServerPort);
                    WriteToStream(tcpClient.GetStream(), $"REGU {LoginBox.Text} {pass}");

                    string data = ReadStringFromStream(tcpClient.GetStream());
                    if (data == "OK")
                    {
                        Login = LoginBox.Text;
                        Password = pass;

                        LoadGroups();
                        ShowSuccessWindow("Successfully registered");
                        var animation = new ThicknessAnimation();
                        animation.From = new Thickness(0, 0, 0, 0);
                        animation.To = new Thickness(0, -500, 0, 500);
                        animation.Duration = TimeSpan.FromSeconds(1);
                        animation.AccelerationRatio = 0.3;
                        animation.DecelerationRatio = 0.7;
                        LogInGrid.BeginAnimation(MarginProperty, animation);
                    }
                    else if (data == "USER")
                    {
                        ShowErrorWindow("Login already used");
                        LoginBox.BeginAnimation(BorderThicknessProperty, errorAnimation);
                    }
                    tcpClient.Close();
                }
                catch (Exception)
                {
                    ShowErrorWindow("Something went wrong");
                }
            }
        }


        //Окна уведомлений
        private void ShowSuccessWindow(string message)
        {
            ThreadPool.QueueUserWorkItem(SuccessWindowAnimation, message);
        }
        private void ShowErrorWindow(string message)
        {
            ThreadPool.QueueUserWorkItem(ErrorWindowAnimation, message);
        }
        private void SuccessWindowAnimation(object obj)
        {
            string message = obj.ToString();
            Action action = () => InfoWindowBorder.Background = new SolidColorBrush(Color.FromRgb(169, 251, 215));
            this.Dispatcher.Invoke(action);
            action = () => InfoWindowText.Foreground = new SolidColorBrush(Color.FromRgb(42, 145, 106));
            this.Dispatcher.Invoke(action);
            action = () => InfoWindowBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 145, 106));
            this.Dispatcher.Invoke(action);

            action = () => InfoWindowText.Content = message;
            this.Dispatcher.Invoke(action);
            action = () => InfoWindow.BeginAnimation(MarginProperty, InAnimation());
            this.Dispatcher.Invoke(action);
            Thread.Sleep(1500);
            action = () => InfoWindow.BeginAnimation(MarginProperty, OutAnimation());
            this.Dispatcher.Invoke(action);
        }
        private void ErrorWindowAnimation(object obj)
        {
            string message = obj.ToString();
            Action action = () => InfoWindowBorder.Background = new SolidColorBrush(Color.FromRgb(236, 131, 133));
            this.Dispatcher.Invoke(action);
            action = () => InfoWindowText.Foreground = new SolidColorBrush(Color.FromRgb(156, 25, 27));
            this.Dispatcher.Invoke(action);
            action = () => InfoWindowBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(156, 25, 27));
            this.Dispatcher.Invoke(action);

            action = () => InfoWindowText.Content = message;
            this.Dispatcher.Invoke(action);
            action = () => InfoWindow.BeginAnimation(MarginProperty, InAnimation());
            this.Dispatcher.Invoke(action);
            Thread.Sleep(1500);
            action = () => InfoWindow.BeginAnimation(MarginProperty, OutAnimation());
            this.Dispatcher.Invoke(action);
        }
        private ThicknessAnimation InAnimation()
        {
            ThicknessAnimation Animanion = new ThicknessAnimation();
            Animanion.From = new Thickness(0, 5, -270, 10);
            Animanion.To = new Thickness(0, 5, 10, 10);
            Animanion.AccelerationRatio = 0.1;
            Animanion.DecelerationRatio = 0.9;
            Animanion.SpeedRatio = 1.3;
            return Animanion;
        }
        private ThicknessAnimation OutAnimation()
        {
            ThicknessAnimation Animanion = new ThicknessAnimation();
            Animanion.From = new Thickness(0, 5, 10, 10);
            Animanion.To = new Thickness(0, 5, -270, 10);
            Animanion.AccelerationRatio = 0.9;
            Animanion.DecelerationRatio = 0.1;
            Animanion.SpeedRatio = 1.3;
            return Animanion;
        }


        //Работа с группами
        private void AddGroup(string name, string title, bool IsTextSelected = false)
        {
            TextBox newTextBox = new TextBox()
            {
                Name = name,
                Text = title,
                IsReadOnly = true,
                Margin = new Thickness(0, 0, -4, 0),
                Padding = new Thickness(0, 5, 0, 5),
                FontFamily = new FontFamily("Century Gothic"),
                FontSize = 20,
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Colors.White),
                Background = new SolidColorBrush(Colors.Transparent),
                SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 168, 232)),
                CaretBrush = new SolidColorBrush(Color.FromRgb(0, 168, 232)),
                SelectionOpacity = 0.3,
                Cursor = Cursors.Arrow,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            newTextBox.MouseDoubleClick += GroupElement_MouseDoubleClick;
            newTextBox.LostFocus += GroupElement_LostFocus;
            newTextBox.GotMouseCapture += GroupElement_GotMouseCapture;
            newTextBox.MouseEnter += GroupElement_MouseEnter;
            newTextBox.MouseLeave += GroupElement_MouseLeave;
            newTextBox.TextChanged += GroupElement_TextChanged;

            if (IsTextSelected)
            {
                newTextBox.Focus();
                newTextBox.IsReadOnly = false;
                newTextBox.SelectAll();
            }

            GroupsPanel.Children.Add(newTextBox);
        }
        private void CreateGroup()
        {
            try
            {
                TcpClient tcpClient = new TcpClient();
                tcpClient.Connect(ServerIp, ServerPort);
                WriteToStream(tcpClient.GetStream(), $"CRTG {Login}");

                string json = ReadStringFromStream(tcpClient.GetStream());
                Classes.Group newGroup = JsonConvert.DeserializeObject<Classes.Group>(json);
                tcpClient.Close();

                Groups.Add(newGroup);
                AddGroup("g" + newGroup.Id, newGroup.Title, true);
            }
            catch (Exception)
            {
                ShowErrorWindow("Something went wrong");
            }
        }
        private void LoadGroups()
        {
            if (Groups == null)
            {
                try
                {
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.Connect(ServerIp, ServerPort);
                    WriteToStream(tcpClient.GetStream(), $"RECG {Login}");

                    string json = ReadStringFromStream(tcpClient.GetStream());
                    Groups = JsonConvert.DeserializeObject<List<Classes.Group>>(json);
                    tcpClient.Close();
                }
                catch (Exception)
                {
                    ShowErrorWindow("Something went wrong");
                }
            }

            if (Groups != null)
            {
                GroupsPanel.Children.Clear();
                foreach (Classes.Group group in Groups)
                {
                    AddGroup("g" + group.Id, group.Title);
                }
            }
        }
        private void SaveGroup(object obj)
        {
            try
            {
                int groupId = (int)obj;
                TcpClient tcpClient = new TcpClient();
                tcpClient.Connect(ServerIp, ServerPort);
                string json = JsonConvert.SerializeObject(Groups.Where(g => g.Id == groupId).FirstOrDefault(), Formatting.Indented);
                WriteToStream(tcpClient.GetStream(), $"SAVG {json}");
                Thread.Sleep(100);
                string answer = ReadStringFromStream(tcpClient.GetStream());
                tcpClient.Close();
                if (answer == "OK")
                {
                    ShowSuccessWindow("Saved");
                    IsTextChanged = false;
                }
                else
                    ShowErrorWindow("Can't save");

            }
            catch (Exception)
            {
                ShowErrorWindow("Something went wrong");
            }
            IsGoingToSave = false;
        }
        private void DeleteGroup(int groupId)
        {
            if (groupId != -1)
            {
                try
                {
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.Connect(ServerIp, ServerPort);
                    WriteToStream(tcpClient.GetStream(), $"DELG {groupId}");
                    tcpClient.Close();

                    Classes.Group bufGroup = Groups.Where(g => g.Id == SelectedGroupId).FirstOrDefault();
                    Groups.Remove(bufGroup);
                    SelectedGroupId = -1;
                    NoteMenuBorder.Visibility = Visibility.Hidden;
                    LoadGroups();
                }
                catch (Exception)
                {
                    ShowErrorWindow("Something went wrong");
                }
            }
            else
            {
                ShowErrorWindow("Group is not selected");
            }
        }


        //Работа с заметками
        private void AddNote(string name, string title, bool _IsChecked, bool IsTextSelected = false)
        {
            CheckBox newCheckBox = new CheckBox()
            {
                Style = TryFindResource("NoteStyle") as Style,
                Name = name,
                Content = title,
                IsChecked = _IsChecked
            };

            newCheckBox.Click += NoteElement_Click;
            newCheckBox.MouseRightButtonDown += NoteElement_MouseRightButtonDown;

            if (IsTextSelected)
            {
                newCheckBox.Focus();
            }

            NotesPanel.Children.Add(newCheckBox);
        }
        private void CreateNote()
        {
            try
            {
                TcpClient tcpClient = new TcpClient();
                tcpClient.Connect(ServerIp, ServerPort);
                WriteToStream(tcpClient.GetStream(), $"CRTN {SelectedGroupId}");

                string json = ReadStringFromStream(tcpClient.GetStream());
                Classes.Note newNote = JsonConvert.DeserializeObject<Classes.Note>(json);
                tcpClient.Close();

                Groups.Where(g => g.Id == SelectedGroupId).FirstOrDefault().Notes.Add(newNote);
                AddNote("n" + newNote.Id, newNote.Title, newNote.IsChecked, true);
            }
            catch (Exception)
            {
                ShowErrorWindow("Something went wrong");
            }
        }
        private void OpenNotesMenu()
        {
            List<Classes.Note> bufNotes = Groups.Where(g => g.Id == SelectedGroupId).Select(g => g.Notes).FirstOrDefault();
            if (bufNotes == null)
            {
                try
                {
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.Connect(ServerIp, ServerPort);

                    WriteToStream(tcpClient.GetStream(), $"RECN {SelectedGroupId}");

                    string json = ReadStringFromStream(tcpClient.GetStream());
                    bufNotes = JsonConvert.DeserializeObject<List<Classes.Note>>(json);
                    tcpClient.Close();
                    Groups.Where(g => g.Id == SelectedGroupId).FirstOrDefault().Notes = bufNotes;
                }
                catch (Exception)
                {
                    ShowErrorWindow("Something went wrong");
                }
            }

            if (bufNotes != null)
            {
                NotesPanel.Children.Clear();
                foreach (Classes.Note note in bufNotes)
                {
                    AddNote("n" + note.Id, note.Title, note.IsChecked);
                }
            }
        }
        private void SaveNote()
        {
            if (IsTextChanged && !IsGoingToSave)
            {
                IsGoingToSave = true;
                timer = new Timer(SaveGroup, SelectedGroupId, 2000, Timeout.Infinite);
            }
        }
        private void DeleteNote(int noteId)
        {
            if (noteId != -1)
            {
                try
                {
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.Connect(ServerIp, ServerPort);
                    WriteToStream(tcpClient.GetStream(), $"DELN {noteId}");
                    tcpClient.Close();

                    Classes.Note bufNote = Groups.Where(g => g.Id == SelectedGroupId).FirstOrDefault().Notes.Where(n => n.Id == noteId).FirstOrDefault();
                    Groups.Where(g => g.Id == SelectedGroupId).FirstOrDefault().Notes.Remove(bufNote);
                    SelectedNoteId = -1;
                    NoteMenu.Visibility = Visibility.Hidden;
                    OpenNotesMenu();
                }
                catch (Exception)
                {
                    ShowErrorWindow("Something went wrong");
                }
            }
            else
            {
                ShowErrorWindow("Note is not selected");
            }
        }


        //Подробное меню заметки
        private void ShowNoteDescription()
        {
            NoteMenu.Visibility = Visibility.Visible;

            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 0;
            animation.To = 1;
            animation.AccelerationRatio = 0.1;
            animation.DecelerationRatio = 0.9;
            animation.Duration = TimeSpan.FromSeconds(0.5);

            try
            {
                Classes.Note note = Groups.Where(g => g.Id == SelectedGroupId).FirstOrDefault().Notes.Where(n => n.Id == SelectedNoteId).FirstOrDefault();
                IsTextCurrentlyChanged = true;
                NoteNameBox.Text = note.Title;
                NoteDescriptionBox.Text = note.Description;
                IsTextCurrentlyChanged = false;
                NoteMenu.BeginAnimation(OpacityProperty, animation);
            }
            catch (Exception)
            {
                ShowErrorWindow("Something went wrong");
                throw;
            }
        }
        private void Border_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
        }
    }
}
