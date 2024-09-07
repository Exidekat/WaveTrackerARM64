using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WaveTracker.Tracker;


namespace WaveTracker.UI
{
    public class InstrumentBank : UserControl
    {
        private Forms.EnterText renameDialog;
        private bool dialogOpen;
        private int lastIndex;
        private int listLength = 32;
        public ScrollViewer scrollbar;

        public int CurrentInstrumentIndex { get; set; }

        // Assuming `Instrument` is part of your project; if not, replace this with the correct type
        public Instrument GetCurrentInstrument {
            get {
                return App.CurrentModule.Instruments[CurrentInstrumentIndex];
            }
        }

        public Button bNewWave, bNewSample, bRemove, bDuplicate, bMoveUp, bMoveDown, bRename;
        public Button bEdit;
        public Menu menu;

        public InstrumentBank()
        {
            // Set up buttons, scrollbar, and other UI elements
            bNewWave = new Button("New Wave", 0, 0, 100);  // Assuming default x, y, and width values
            bNewWave.Click += (s, e) => AddWave();

            bNewSample = new Button("New Sample", 0, 0, 100);
            bNewSample.Click += (s, e) => AddSample();

            bRemove = new Button("Remove", 0, 0, 100);
            bRemove.Click += (s, e) => RemoveInstrument();

            bDuplicate = new Button("Duplicate", 0, 0, 100);
            bDuplicate.Click += (s, e) => DuplicateInstrument();

            bMoveUp = new Button("Move Up", 0, 0, 100);
            bMoveUp.Click += (s, e) => MoveUp();

            bMoveDown = new Button("Move Down", 0, 0, 100);
            bMoveDown.Click += (s, e) => MoveDown();

            bEdit = new Button("Edit", 0, 0, 100);
            bEdit.Click += (s, e) => Edit();

            bRename = new Button("Rename", 0, 0, 100);
            bRename.Click += (s, e) => Rename();

            scrollbar = new ScrollViewer();
            this.Content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Children = {
                    new TextBlock { Text = "Instrument Bank", FontSize = 16, FontWeight = FontWeight.Bold },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children = {
                            bNewWave, bNewSample, bRemove, bDuplicate, bMoveUp, bMoveDown, bEdit, bRename
                        }
                    },
                    scrollbar
                }
            };
        }

        public Menu CreateInstrumentMenu() {
            return new Menu {
                Items = new MenuItem[] {
                    new MenuItem { Header = "Add Wave Instrument", Command = ReactiveCommand.Create(AddWave) },
                    new MenuItem { Header = "Add Sample Instrument", Command = ReactiveCommand.Create(AddSample) },
                    new MenuItem { Header = "Remove Instrument", Command = ReactiveCommand.Create(RemoveInstrument) },
                    new MenuItem { Header = "Duplicate Instrument", Command = ReactiveCommand.Create(DuplicateInstrument) },
                    new MenuItem { Header = "Move Up", Command = ReactiveCommand.Create(MoveUp) },
                    new MenuItem { Header = "Move Down", Command = ReactiveCommand.Create(MoveDown) },
                    new MenuItem { Header = "Rename", Command = ReactiveCommand.Create(Rename) },
                    new MenuItem { Header = "Edit", Command = ReactiveCommand.Create(Edit) }
                }
            };
        }

        public void Update() {
            // Update the UI based on instrument list
            listLength = (int)(scrollbar.Viewport.Height / 11);
            scrollbar.Height = listLength * 11;

            if (App.Shortcuts["General\\Next instrument"].IsPressedRepeat) {
                CurrentInstrumentIndex++;
                CurrentInstrumentIndex = Math.Clamp(CurrentInstrumentIndex, 0, App.CurrentModule.Instruments.Count - 1);
                MoveBounds();
                if (App.InstrumentEditor.IsOpen) {
                    Edit();
                }
            }
            if (App.Shortcuts["General\\Previous instrument"].IsPressedRepeat) {
                CurrentInstrumentIndex--;
                CurrentInstrumentIndex = Math.Clamp(CurrentInstrumentIndex, 0, App.CurrentModule.Instruments.Count - 1);
                MoveBounds();
                if (App.InstrumentEditor.IsOpen) {
                    Edit();
                }
            }

            // Scroll handling and updates
            scrollbar.Offset = new Vector(0, CurrentInstrumentIndex * 11);
            MoveBounds();
        }

        private async void StartRenameDialog() {
            renameDialog = new Forms.EnterText();
            renameDialog.TextBoxInput.Text = GetCurrentInstrument.name;
            renameDialog.Title = $"Rename Instrument {CurrentInstrumentIndex:D2}";
            var result = await renameDialog.ShowDialog<string>(App.MainWindow);
            if (result != null && result != "\tcanceled") {
                App.CurrentModule.Instruments[CurrentInstrumentIndex].SetName(Helpers.FlushString(result));
                App.CurrentModule.SetDirty();
            }
        }

        public void AddWave() {
            App.CurrentModule.Instruments.Add(new WaveInstrument());
            App.CurrentModule.SetDirty();
            CurrentInstrumentIndex = App.CurrentModule.Instruments.Count - 1;
            MoveBounds();
        }

        public void AddSample() {
            App.CurrentModule.Instruments.Add(new SampleInstrument());
            App.CurrentModule.SetDirty();
            CurrentInstrumentIndex = App.CurrentModule.Instruments.Count - 1;
            MoveBounds();
        }

        public void DuplicateInstrument() {
            App.CurrentModule.Instruments.Add(GetCurrentInstrument.Clone());
            App.CurrentModule.SetDirty();
            MoveBounds();
        }

        public void MoveUp() {
            App.CurrentModule.SwapInstrumentsInSongs(CurrentInstrumentIndex, CurrentInstrumentIndex - 1);
            App.CurrentModule.Instruments.Reverse(CurrentInstrumentIndex - 1, 2);
            App.CurrentModule.SetDirty();
            CurrentInstrumentIndex--;
            MoveBounds();
        }

        public void MoveDown() {
            App.CurrentModule.SwapInstrumentsInSongs(CurrentInstrumentIndex, CurrentInstrumentIndex + 1);
            App.CurrentModule.Instruments.Reverse(CurrentInstrumentIndex, 2);
            App.CurrentModule.SetDirty();
            CurrentInstrumentIndex++;
            MoveBounds();
        }

        public void RemoveInstrument() {
            App.CurrentModule.AdjustForDeletedInstrument(CurrentInstrumentIndex);
            App.CurrentModule.Instruments.RemoveAt(CurrentInstrumentIndex);
            App.CurrentModule.SetDirty();
            if (CurrentInstrumentIndex >= App.CurrentModule.Instruments.Count) {
                CurrentInstrumentIndex = App.CurrentModule.Instruments.Count - 1;
            }
            MoveBounds();
        }

        public void Rename() {
            if (!dialogOpen) {
                dialogOpen = true;
                StartRenameDialog();
            }
        }

        public void Edit() {
            App.InstrumentEditor.Open(GetCurrentInstrument, CurrentInstrumentIndex);
        }

        private void MoveBounds() {
            // Logic to ensure the current instrument is visible within the bounds of the scrollable area
            if (CurrentInstrumentIndex > scrollbar.Offset.Y + listLength - 1) {
                scrollbar.Offset = new Vector(0, CurrentInstrumentIndex - listLength + 1);
            }
            if (CurrentInstrumentIndex < scrollbar.Offset.Y) {
                scrollbar.Offset = new Vector(0, CurrentInstrumentIndex);
            }
        }
    }
}
