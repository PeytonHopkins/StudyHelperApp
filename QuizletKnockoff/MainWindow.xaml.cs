// Peyton Luke Hopkins
// Study Helper App

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// TODO: Start working on allowing for multiple files to be saved.

namespace QuizletKnockoff
{
    public partial class MainWindow : Window
    {
        private bool isTerm = true;
        private bool settingsEnabled = false;
        private bool shouldSave = false;

        public record struct TermRecord(int index, string termText, string defText, bool isKnown);
        //private Stack<TermRecord> termStack = new Stack<TermRecord>(); Will be utilized in later versions.
        private Queue<TermRecord> termQueue = new Queue<TermRecord>(); //Order of Terms being shown to user
        private List<TermRecord> termList = new List<TermRecord>(); // Used for random sorting in queue
        private ObservableCollection<TermRecord> terms = new ObservableCollection<TermRecord>();
        public TermRecord currentTerm;

        string fileName = "";
        string jsonFileName = "";
        JsonSerializerOptions jsonOptions = new JsonSerializerOptions();

        int knownCount = 0;
        int learningCount = 0;

        public MainWindow()
        {
            LoadData();
            InitializeComponent();

            OrderComboBox.SelectionChanged += OrderComboBox_SelectionChanged;
            AnsWithComboBox.SelectionChanged += AnsWithComboBox_SelectionChanged;

            jsonOptions.WriteIndented = true;

            KnowCountLabel.Content = knownCount;
            LearningLabel.Content = learningCount;

            if (learningCount == 0)
            {
                FlashCardBottomGrid.IsEnabled = false;
                FlashCard_Txt.Text = "All Terms Known!\nGood Job! \nStart over by clicking \"Reset\" in the settings tab.";
            }
        }

        private void FlashcardFlip(object sender, RoutedEventArgs e) // Basic mechanics to flip between term and definition
        {
            if (isTerm)
            {
                FlashCard_Txt.Text = currentTerm.defText;
                isTerm = false;
            }
            else if (!isTerm)
            {
                FlashCard_Txt.Text = currentTerm.termText;
                isTerm = true;
            }
        }

        private void FlashcardFlip_Btn_KeyUp(object sender, KeyEventArgs e) // Checks for button input to flip the flashcard
        {
            if (e.Key == Key.Space)
            {
                FlashcardFlip(sender, e);
            }
        }

        private void Know_Btn_Click(object sender, RoutedEventArgs e) // Logic for when a term is known
        {
            if (termQueue.Count > 0) // If terms are still in queue
            {
                TermRecord t = terms[currentTerm.index - 1]; // Update term to known
                t.isKnown = true;
                terms[currentTerm.index - 1] = t;

                knownCount++; // Update UI text & variables
                learningCount--;
                LearningLabel.Content = learningCount;
                KnowCountLabel.Content = knownCount;

                FlashCardLoad(); // Load next flashcard
            }
            else if (termQueue.Count == 0) // If all terms are known
            {
                if (learningCount > 0) // If the variable hasnt been updated 
                {
                    TermRecord t = terms[currentTerm.index - 1]; // Update term to known
                    t.isKnown = true;
                    terms[currentTerm.index - 1] = t;

                    knownCount++; // Update UI text & variables
                    learningCount--;
                    LearningLabel.Content = learningCount;
                    KnowCountLabel.Content = knownCount;
                }

                FlashCard_Txt.Text = "All Terms Known!\nGood Job! \nStart over by clicking \"Reset\" in the settings tab.";
                FlashCardBottomGrid.IsEnabled = false; // Disable buttons as they are no longer needed
            }
        }

        private void Still_Learning_Btn_Click(object sender, RoutedEventArgs e) // Logic for when the card is not memorized
        {
            TermRecord tempTerm = currentTerm; // Requeues the card and goes onto the next in the queue
            termQueue.Enqueue(tempTerm);
            FlashCardLoad();
        }

        private void FlashCardGrid_Initialized(object sender, EventArgs e) // Loads the first flashcard on initialization
        {
            FlashCardLoad();
        }

        private void FlashCardLoad() // Loads the next card in the queue and updates the flashcard texts
        {
            if (termQueue.Count > 0)
            {
                TermRecord t = termQueue.Dequeue();
                currentTerm = t;
                LoadSide(t);
            }
        }
        private void LoadSide(TermRecord t) // Loads the "side" of the flashcard that the user has selected.
        {
            ComboBox ansBox = AnsWithComboBox;
            Random r = new Random();
            int ranNum;

            if (ansBox.SelectedIndex == 0) // Def
            {
                FlashCard_Txt.Text = t.defText;
                isTerm = false;
            }
            if (ansBox.SelectedIndex == 1) // Term
            {
                FlashCard_Txt.Text = t.termText;
                isTerm = true;
            }
            if (ansBox.SelectedIndex == 2) // Random
            {
                ranNum = r.Next(2);

                if (ranNum == 0)
                {
                    FlashCard_Txt.Text = t.termText;
                    isTerm = true;
                }
                else if (ranNum == 1)
                {
                    FlashCard_Txt.Text = t.defText;
                    isTerm = false;
                }
            }
        }
        private void LoadData()
        {
            fileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\QuizletKnockoff";
            jsonFileName = fileName + "\\terms.json";

            try
            {
                if (!Directory.Exists(fileName)) // If it is the first time running the program
                {
                    Directory.CreateDirectory(fileName); // Create base folder in AppData
                    File.Create(jsonFileName);  // Create a json file in the folder to store the terms
                }
                else // if it is a returning user
                {
                    StreamReader r = new StreamReader(jsonFileName); // read JSON data
                    string s = r.ReadToEnd();
                    r.Close();

                    if (s.Length > 3) // If the json file has data
                    {
                        try
                        {
                            terms = JsonSerializer.Deserialize<ObservableCollection<TermRecord>>(s);

                            foreach (TermRecord t in terms) // Enqueue all terms that haven't been studied yet
                            {
                                if (!t.isKnown)
                                {
                                    termQueue.Enqueue(t);
                                    learningCount++;
                                }
                                else if (t.isKnown)
                                {
                                    knownCount++;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Error reading file data");
                            MessageBox.Show(e.Message);
                        }

                    }
                    else // if the json file is empty, add a term to instruct the new user.
                    {
                        TermRecord t = new TermRecord(1, "Welcome to QuizletRipoff!", "Add terms to get started.", false);
                        terms.Add(t);
                        termQueue.Enqueue(t);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

        }

        private void Edit_Cards_Btn_Click(object sender, RoutedEventArgs e) // Loads the Edit Cards tab
        {
            FlashCardGrid.IsEnabled = false;
            FlashCardGrid.Visibility = Visibility.Collapsed;

            EditCardsGrid.IsEnabled = true;
            EditCardsGrid.Visibility = Visibility.Visible;

            TermListBox.ItemsSource = terms;
        }

        private void Edit_Cards_Back_Btn_Click(object sender, RoutedEventArgs e) // Returns from Edit tab to Flashcard tab
        {
            if (shouldSave) // If a change has occured, save the data.
            {
                SaveData();
                shouldSave = false;
            }

            EditCardsGrid.IsEnabled = false;
            EditCardsGrid.Visibility = Visibility.Collapsed;

            FlashCardGrid.IsEnabled = true;
            FlashCardGrid.Visibility = Visibility.Visible;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e) // If a term is to be deleted
        {
            if (terms.Count > 1)
            {
                Grid grid = (Grid)LogicalTreeHelper.GetParent((DependencyObject)sender); // Get the term that was clicked to get its index
                Label label = new Label();
                label = (Label)LogicalTreeHelper.FindLogicalNode(grid, "index");
                int i = Convert.ToInt16(label.Content);

                if (terms[i - 1].isKnown) // Ensures known count is still accurate.
                {
                    knownCount--;
                }
                else if (!terms[i - 1].isKnown)
                {
                    learningCount--;
                }

                KnowCountLabel.Content = knownCount;
                LearningLabel.Content = learningCount;

                terms.RemoveAt((i - 1));
                shouldSave = true;

                for (int j = terms.Count - i, k = i; j >= 0; j--, k++) // Decreases the index of each term after the deleted term to prevent index gaps
                {
                    TermRecord t = terms[k - 1];
                    t.index--;
                    terms[k - 1] = t;
                }


            }
        }
        private void AddTerm_Button_Click(object sender, RoutedEventArgs e) // Adds an empty term to the next index
        {
            Grid grid = (Grid)LogicalTreeHelper.GetParent((DependencyObject)sender); // Gets current term
            Label label = new Label();
            label = (Label)LogicalTreeHelper.FindLogicalNode(grid, "index");
            int i = Convert.ToInt16(label.Content);

            TermRecord t = new TermRecord(i + 1, " ", " ", false); // Create empty term and insert it after current
            terms.Insert(i, t);
            shouldSave = true;

            learningCount++;
            LearningLabel.Content = learningCount;

            for (int j = terms.Count - i - 1, k = i; j > 0; j--, k++) // Increment index of following terms
            {
                TermRecord term = terms[k + 1];
                term.index++;
                terms[k + 1] = term;
            }

        }

        private void defText_LostFocus(object sender, RoutedEventArgs e) // Update definition data when textbox lost focus
        {
            try
            {
                Grid grid = (Grid)LogicalTreeHelper.GetParent((DependencyObject)sender); // Get the terms info
                TextBox defTextBox = (TextBox)LogicalTreeHelper.FindLogicalNode(grid, "defText");
                Label indexLabel = (Label)LogicalTreeHelper.FindLogicalNode(grid, "index");
                int i = Convert.ToInt16(indexLabel.Content) - 1;

                if (defTextBox.Text != terms[i].defText) // If the text changed, save it
                {
                    if (terms[i].isKnown) // Update variables to reflect term now being unlearned
                    {
                        knownCount--;
                        learningCount++;
                        KnowCountLabel.Content = knownCount;
                        LearningLabel.Content = learningCount;
                    }

                    TermRecord t = terms[i]; // Update term object
                    t.isKnown = false;
                    t.defText = defTextBox.Text;
                    terms[i] = t;
                    shouldSave = true;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void termText_LostFocus(object sender, RoutedEventArgs e) // Update term data when textbox lost focus
        {
            try
            {
                Grid grid = (Grid)LogicalTreeHelper.GetParent((DependencyObject)sender); // Get the term info
                TextBox termTextBox = (TextBox)LogicalTreeHelper.FindLogicalNode(grid, "termText");
                Label indexLabel = (Label)LogicalTreeHelper.FindLogicalNode(grid, "index");
                int i = Convert.ToInt16(indexLabel.Content) - 1;

                if (termTextBox.Text != terms[i].termText) // If the text changed, save it
                {
                    if (terms[i].isKnown) // Update variables to reflect term now being unlearned
                    {
                        knownCount--;
                        learningCount++;
                        KnowCountLabel.Content = knownCount;
                        LearningLabel.Content = learningCount;
                    }

                    TermRecord t = terms[i]; // Update term object
                    t.isKnown = false;
                    t.termText = termTextBox.Text;
                    terms[i] = t;
                    shouldSave = true;

                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void Settings_Btn_Click(object sender, RoutedEventArgs e) // Logic for the Settings tab for Flashcards
        {
            if (!settingsEnabled)
            {
                FlashCardSettingsGrid.IsEnabled = true;
                FlashCardSettingsGrid.Visibility = Visibility.Visible;

                settingsEnabled = true;
            }
            else if (settingsEnabled)
            {
                FlashCardSettingsGrid.IsEnabled = false;
                FlashCardSettingsGrid.Visibility = Visibility.Collapsed;

                settingsEnabled = false;
            }
        }

        private void OrderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadFlashCardOrder();
        }

        private void SaveData() // Saves terms to the JSON file
        {
            string s = JsonSerializer.Serialize(terms, jsonOptions);

            StreamWriter sw = new StreamWriter(jsonFileName);
            sw.Write(s);
            sw.Close();

            LoadFlashCardOrder();
        }

        private void AnsWithComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadSide(currentTerm);
        }

        private void MainWindowObj_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveData();
        }

        private void ChangeList_Btn_Click(object sender, RoutedEventArgs e)
        {
            EditCardsGrid.IsEnabled = false;
            EditCardsGrid.Visibility = Visibility.Hidden;

            TermsGrid.IsEnabled = true;
            TermsGrid.Visibility = Visibility.Visible;
        }

        private void Reset_Btn_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < terms.Count; i++)
            {
                TermRecord t = terms[i];
                t.isKnown = false;
                terms[i] = t;
            }

            FlashCardBottomGrid.IsEnabled = true;

            learningCount += knownCount;
            knownCount = 0;

            SaveData();

            KnowCountLabel.Content = "0";
            LearningLabel.Content = learningCount;
        }

        private void Terms_Grid_Back_Btn_Click(object sender, RoutedEventArgs e)
        {
            TermsGrid.IsEnabled = false;
            TermsGrid.Visibility = Visibility.Collapsed;

            EditCardsGrid.IsEnabled = true;
            EditCardsGrid.Visibility = Visibility.Visible;
        }

        private void LoadFlashCardOrder() // Loads the queue of flash cards in the selected order in settings
        {
            ComboBox comboBox = OrderComboBox;

            if (comboBox.SelectedIndex == 0) // In order selected index
            {
                termQueue.Clear();
                foreach (TermRecord t in terms)
                {
                    if (!t.isKnown)
                    {
                        termQueue.Enqueue(t);
                    }
                }
                FlashCardLoad();
            }
            else if (comboBox.SelectedIndex == 1) // Randomized order selected index
            {
                termQueue.Clear();
                termList.Clear();

                Random r = new Random();
                TermRecord ranTerm;
                int ranNum;

                foreach (TermRecord t in terms)
                {
                    if (!t.isKnown)
                    {
                        termList.Add(t);
                    }
                }

                int length = termList.Count;

                for (int i = length; i > 0; i--)
                {
                    ranNum = r.Next(i);
                    ranTerm = termList[ranNum];
                    termList.RemoveAt(ranNum);
                    termQueue.Enqueue(ranTerm);
                }

                FlashCardLoad();
            }

        }
    }
}
