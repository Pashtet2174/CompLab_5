using System.Text;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Comp_Lab1;
using FastColoredTextBoxNS;
namespace Comp_Lab1;

public partial class Form1 : Form
{
    Style BlueStyle = new TextStyle(Brushes.Blue, null, FontStyle.Bold);
    Style BrownStyle = new TextStyle(Brushes.Brown, null, FontStyle.Regular); 
    
    public Form1()
    {
        
        InitializeComponent();
        this.AllowDrop = true;
        this.DragEnter += Form1_DragEnter;
        this.DragDrop += Form1_DragDrop;
        dgvErrors.MouseEnter += (s, e) => dgvErrors.Focus(); 
        dgvErrors.MouseWheel += DgvErrors_MouseWheel;
        EnableDragDropForAll(this);
        ApplyLocalization();
        dgvErrors.CellClick += dgvErrors_CellClick;
        // В конструктор Form1()
        ContextMenuStrip tabMenu = new ContextMenuStrip();
        ToolStripMenuItem closeTabItem = new ToolStripMenuItem("Закрыть вкладку");
        closeTabItem.Click += CloseTab_Click;
        tabMenu.Items.Add(closeTabItem);
        tabControlEditor.ContextMenuStrip = tabMenu;
        tabControlEditor.MouseDown += (s, e) => {
            if (e.Button == MouseButtons.Right) {
                for (int i = 0; i < tabControlEditor.TabCount; i++) {
                    if (tabControlEditor.GetTabRect(i).Contains(e.Location)) {
                        tabControlEditor.SelectedIndex = i;
                        break;
                    }
                }
            }
        };
    }
    private void EnableDragDropForAll(Control parent)
    {
        foreach (Control c in parent.Controls)
        {
            c.AllowDrop = true;
            c.DragEnter += Form1_DragEnter;
            c.DragDrop += Form1_DragDrop;
        
            if (c.HasChildren)
            {
                EnableDragDropForAll(c);
            }
        }
    }


    private void Form1_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void Form1_DragDrop(object sender, DragEventArgs e)
    {
        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        foreach (string filePath in files)
        {
            try
            {
                string content = System.IO.File.ReadAllText(filePath);
                CreateNewTab(filePath, content);
            }
            catch (Exception ex) { MessageBox.Show(Label.ErrPrefix + ex.Message); }
        }
    }
    private void CreateFile(object sender = null, EventArgs e = null)
    {
        CreateNewTab(Label.NewFile);
    }

    private void OpenFile(object sender = null, EventArgs e = null)
    {
        OpenFileDialog ofd = new OpenFileDialog { Filter = "Text Files|*.txt|All Files|*.*",InitialDirectory = Application.StartupPath };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            string content = System.IO.File.ReadAllText(ofd.FileName);
            CreateNewTab(ofd.FileName, content);
        }
    }
    private void SaveFile(object sender = null, EventArgs e = null) {
        if (tabControlEditor.SelectedTab == null || CurrentEditor == null) return;

        string path = tabControlEditor.SelectedTab.Tag as string;

        if (string.IsNullOrEmpty(path) || path == Label.NewFile) {
            SaveFileAs(); 
        } else {
            System.IO.File.WriteAllText(path, CurrentEditor.Text);
            CurrentEditor.IsChanged = false;
            tabControlEditor.SelectedTab.Text = System.IO.Path.GetFileName(path);
            lblStatus.Text = Label.FileSaved;
        }
    }
    private void SaveFileAs(object sender = null, EventArgs e = null) {
        if (tabControlEditor.SelectedTab == null) return;

        SaveFileDialog sfd = new SaveFileDialog { Filter = "Text Files|*.txt|All Files|*.*" , InitialDirectory = Application.StartupPath};
        if (sfd.ShowDialog() == DialogResult.OK) {
            tabControlEditor.SelectedTab.Tag = sfd.FileName;
            tabControlEditor.SelectedTab.Text = System.IO.Path.GetFileName(sfd.FileName);
        
            SaveFile();
        }
    }
    private void ExitApp(object sender = null, EventArgs e = null)
    {
        this.Close();
    }
    
    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        foreach (TabPage tab in tabControlEditor.TabPages)
        {
            var fctb = tab.Controls[0] as FastColoredTextBox;
            if (fctb != null && fctb.IsChanged)
            {
                tabControlEditor.SelectedTab = tab; 
                var result = MessageBox.Show(string.Format(Label.SaveConfirmMsg, tab.Text), Label.ExitTitle, 
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Yes) SaveFile();
                else if (result == DialogResult.Cancel) { e.Cancel = true; return; }
            }
        }
    }   
   private void RunParser(object sender = null, EventArgs e = null)
    {
        if (tabControlEditor.TabCount == 0 || tabControlEditor.SelectedTab == null)
        {
            lblStatus.Text = Label.ErrNoTabsStatus;
            lblStatus.ForeColor = Color.OrangeRed;
        
            MessageBox.Show(Label.ErrNoTabsMsg, 
                Label.ErrNoTabsTitle, 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(CurrentEditor.Text))
        {
            lblStatus.Text = Label.ErrEmptyStatus;
            lblStatus.ForeColor = Color.OrangeRed;

            MessageBox.Show(Label.ErrEmptyMsg, 
                Label.ErrEmptyTitle, 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Warning);
            return;
        }

        dgvErrors.Rows.Clear();
        rtbAstOutput.Clear(); // Очищаем поле вывода AST перед новым запуском

        // 1. Лексический анализ
        var scanner = new Scanner(CurrentEditor.Text);
        var allTokens = scanner.Analyze();

        // 2. Синтаксический анализ
        var parser = new Parser(allTokens);
        parser.Analyze();
        
        // Вывод синтаксических ошибок
        foreach (var err in parser.Errors)
        {
            AddErrorToGrid(err.Message, err.Token.Value, err.Token);
        }

        // 3. Семантический анализ и построение AST (запускаем в любом случае, чтобы найти все возможные ошибки)
        var semanticAnalyzer = new SemanticAnalyzer();
        semanticAnalyzer.Analyze(allTokens);

        // Вывод семантических ошибок
        foreach (var err in semanticAnalyzer.Errors)
        {
            // Передаем сообщение, строку и позицию
            AddSemanticErrorToGrid(err.Message, err.Line, err.Position);
        }

        // Вывод построенного AST дерева
        if (semanticAnalyzer.AstRoots.Any())
        {
            var sb = new StringBuilder();
            foreach (var root in semanticAnalyzer.AstRoots)
            {
                root.Print(sb, "", true);
            }
            rtbAstOutput.Text = sb.ToString();
        }

        // Итоги
        int totalErrors = parser.Errors.Count + semanticAnalyzer.Errors.Count;

        if (totalErrors == 0)
        {
            lblStatus.Text = Label.ParserSuccessStatus;
            lblStatus.ForeColor = Color.Green;
            MessageBox.Show(Label.ParserSuccessMsg, 
                Label.ParserResultTitle, 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Information);
        }
        else
        {
            lblStatus.Text = string.Format(Label.ParserErrorCount, totalErrors);
            lblStatus.ForeColor = Color.Red; 
        }
    }
    private void AddErrorToGrid( string typeName, string value, Token token)
    {
        string location = string.Format(Label.ErrorLocationFormat, token.Line, token.StartPos);
        int rowIndex = dgvErrors.Rows.Add( value, location, typeName);
        dgvErrors.Rows[rowIndex].Tag = token;

        dgvErrors.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightCoral;
        dgvErrors.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Black;
    }
    private void AddSemanticErrorToGrid(string message, int line, int pos)
    {
        string location = string.Format(Label.ErrorLocationFormat, line, pos);
    
        // В dgvErrors порядок столбцов: (Значение/Сообщение, Локация, Тип)
        int rowIndex = dgvErrors.Rows.Add(message, location, "Семантическая ошибка");
    
        // Для семантических ошибок тег токена может быть null, так как мы берем данные напрямую
        dgvErrors.Rows[rowIndex].Tag = null; 

        // Используем другой оттенок красного, чтобы визуально отличать от ошибок синтаксиса
        dgvErrors.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightSalmon;
        dgvErrors.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Black;
    }
    private void ShowAbout(object sender, EventArgs e) {
        MessageBox.Show(Label.AboutMsg, Label.MenuAbout);
    }
    
    private void Help_Click(object sender, EventArgs e) => OpenHtml("help.html");
    private void Task_Click(object sender, EventArgs e) => OpenHtml("task.html");
    private void Grammar_Click(object sender, EventArgs e) => OpenHtml("grammar.html");
    private void Classification_Click(object sender, EventArgs e) => OpenHtml("classification.html");
    private void Method_Click(object sender, EventArgs e) => OpenHtml("method.html");
    private void Example_Click(object sender, EventArgs e) => OpenHtml("example.html");
    private void Literature_Click(object sender, EventArgs e) => OpenHtml("literature.html");
    private void SourceCode_Click(object sender, EventArgs e) => OpenHtml("code.html");
    private void OpenHtml(string fileName)
    {
        try
        {
            string langCode = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
        
            string path = System.IO.Path.Combine(Application.StartupPath, "html", langCode, fileName);
            if (System.IO.File.Exists(path))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path)
                {
                    UseShellExecute = true 
                });
            }
            else
            {
                // Если файла нет, выскочит окно с путем. 
                // Вы сможете открыть этот путь в проводнике и проверить, есть ли там файл.
                MessageBox.Show(string.Format(Label.FileNotFoundMsg, path), 
                    Label.ErrPrefix.TrimEnd(':', ' '), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(Label.ErrOpenPage + ex.Message);
        }
    }
    private void Undo_Click(object sender, EventArgs e) {
        if (CurrentEditor != null && CurrentEditor.UndoEnabled) 
            CurrentEditor.Undo();
    }

    private void Redo_Click(object sender, EventArgs e) {
        if (CurrentEditor != null && CurrentEditor.RedoEnabled) 
            CurrentEditor.Redo();
    }

    private void Cut_Click(object sender, EventArgs e) {
        if (CurrentEditor != null) 
            CurrentEditor.Cut();
    }

    private void Copy_Click(object sender, EventArgs e) {
        if (CurrentEditor != null) 
            CurrentEditor.Copy();
    }

    private void Paste_Click(object sender, EventArgs e) {
        if (CurrentEditor != null) 
            CurrentEditor.Paste();
    }

    private void Delete_Click(object sender, EventArgs e) {
        if (CurrentEditor != null) 
            CurrentEditor.SelectedText = "";
    }

    private void SelectAll_Click(object sender, EventArgs e) {
        if (CurrentEditor != null) 
            CurrentEditor.SelectAll();
    }
    private FastColoredTextBox CreateNewTab(string fileName = "", string content = "")
    {
        string displayName = string.IsNullOrEmpty(fileName) ? Label.NewFile : System.IO.Path.GetFileName(fileName);
        TabPage newTabPage = new TabPage(displayName);
    
        newTabPage.Tag = (string.IsNullOrEmpty(fileName) || fileName == Label.NewFile) ? "" : fileName;
        FastColoredTextBox fctb = new FastColoredTextBox();
        fctb.Dock = DockStyle.Fill;
        fctb.Language = FastColoredTextBoxNS.Language.Custom;
        fctb.Text = content;

        fctb.HotkeysMapping.Clear();

        // --- Одиночные клавиши ---
        fctb.HotkeysMapping.Add(Keys.Tab, FCTBAction.IndentIncrease);
        fctb.HotkeysMapping.Add(Keys.Escape, FCTBAction.ClearHints);
        fctb.HotkeysMapping.Add(Keys.PageUp, FCTBAction.GoPageUp);
        fctb.HotkeysMapping.Add(Keys.PageDown, FCTBAction.GoPageDown);
        fctb.HotkeysMapping.Add(Keys.End, FCTBAction.GoEnd);
        fctb.HotkeysMapping.Add(Keys.Home, FCTBAction.GoHome);
        fctb.HotkeysMapping.Add(Keys.Left, FCTBAction.GoLeft);
        fctb.HotkeysMapping.Add(Keys.Up, FCTBAction.GoUp);
        fctb.HotkeysMapping.Add(Keys.Right, FCTBAction.GoRight);
        fctb.HotkeysMapping.Add(Keys.Down, FCTBAction.GoDown);
        fctb.HotkeysMapping.Add(Keys.Insert, FCTBAction.ReplaceMode);
        fctb.HotkeysMapping.Add(Keys.Delete, FCTBAction.DeleteCharRight);
        fctb.HotkeysMapping.Add(Keys.F3, FCTBAction.FindNext);

        // --- Shift + Клавиши (Выделение и правка) ---
        fctb.HotkeysMapping.Add(Keys.Tab | Keys.Shift, FCTBAction.IndentDecrease);
        fctb.HotkeysMapping.Add(Keys.PageUp | Keys.Shift, FCTBAction.GoPageUpWithSelection);
        fctb.HotkeysMapping.Add(Keys.PageDown | Keys.Shift, FCTBAction.GoPageDownWithSelection);
        fctb.HotkeysMapping.Add(Keys.End | Keys.Shift, FCTBAction.GoEndWithSelection);
        fctb.HotkeysMapping.Add(Keys.Home | Keys.Shift, FCTBAction.GoHomeWithSelection);
        fctb.HotkeysMapping.Add(Keys.Left | Keys.Shift, FCTBAction.GoLeftWithSelection);
        fctb.HotkeysMapping.Add(Keys.Up | Keys.Shift, FCTBAction.GoUpWithSelection);
        fctb.HotkeysMapping.Add(Keys.Right | Keys.Shift, FCTBAction.GoRightWithSelection);
        fctb.HotkeysMapping.Add(Keys.Down | Keys.Shift, FCTBAction.GoDownWithSelection);
        fctb.HotkeysMapping.Add(Keys.Insert | Keys.Shift, FCTBAction.Paste);
        fctb.HotkeysMapping.Add(Keys.Delete | Keys.Shift, FCTBAction.Cut);

        // --- CTRL + Клавиши (Навигация и Буфер) ---
        fctb.HotkeysMapping.Add(Keys.Back | Keys.Control, FCTBAction.ClearWordLeft); 

        fctb.HotkeysMapping.Add(Keys.Space | Keys.Control, FCTBAction.AutocompleteMenu);
        fctb.HotkeysMapping.Add(Keys.End | Keys.Control, FCTBAction.GoLastLine);
        fctb.HotkeysMapping.Add(Keys.Home | Keys.Control, FCTBAction.GoFirstLine);
        fctb.HotkeysMapping.Add(Keys.Left | Keys.Control, FCTBAction.GoWordLeft);
        fctb.HotkeysMapping.Add(Keys.Right | Keys.Control, FCTBAction.GoWordRight);
        fctb.HotkeysMapping.Add(Keys.Up | Keys.Control, FCTBAction.ScrollUp);
        fctb.HotkeysMapping.Add(Keys.Down | Keys.Control, FCTBAction.ScrollDown);
        fctb.HotkeysMapping.Add(Keys.Insert | Keys.Control, FCTBAction.Copy);
        fctb.HotkeysMapping.Add(Keys.Delete | Keys.Control, FCTBAction.ClearWordRight);

        // --- CTRL + Буквы (Стандартные команды) ---
        fctb.HotkeysMapping.Add(Keys.D0 | Keys.Control, FCTBAction.ZoomNormal);
        fctb.HotkeysMapping.Add(Keys.A | Keys.Control, FCTBAction.SelectAll);
        fctb.HotkeysMapping.Add(Keys.B | Keys.Control, FCTBAction.BookmarkLine);
        fctb.HotkeysMapping.Add(Keys.C | Keys.Control, FCTBAction.Copy);
        fctb.HotkeysMapping.Add(Keys.E | Keys.Control, FCTBAction.MacroExecute);
        fctb.HotkeysMapping.Add(Keys.F | Keys.Control, FCTBAction.FindDialog);
        fctb.HotkeysMapping.Add(Keys.G | Keys.Control, FCTBAction.GoToDialog);
        fctb.HotkeysMapping.Add(Keys.H | Keys.Control, FCTBAction.ReplaceDialog);
        fctb.HotkeysMapping.Add(Keys.I | Keys.Control, FCTBAction.AutoIndentChars);
        fctb.HotkeysMapping.Add(Keys.M | Keys.Control, FCTBAction.MacroRecord);
        fctb.HotkeysMapping.Add(Keys.N | Keys.Control, FCTBAction.GoNextBookmark);
        fctb.HotkeysMapping.Add(Keys.R | Keys.Control, FCTBAction.Redo);
        fctb.HotkeysMapping.Add(Keys.U | Keys.Control, FCTBAction.UpperCase);
        fctb.HotkeysMapping.Add(Keys.V | Keys.Control, FCTBAction.Paste);
        fctb.HotkeysMapping.Add(Keys.X | Keys.Control, FCTBAction.Cut);
        fctb.HotkeysMapping.Add(Keys.Z | Keys.Control, FCTBAction.Undo);

        // --- Клавиатура Zoom и Навигация ---
        fctb.HotkeysMapping.Add(Keys.Add | Keys.Control, FCTBAction.ZoomIn);
        fctb.HotkeysMapping.Add(Keys.Subtract | Keys.Control, FCTBAction.ZoomOut);
        fctb.HotkeysMapping.Add(Keys.OemMinus | Keys.Control, FCTBAction.NavigateBackward);

        // --- CTRL + SHIFT ---
        fctb.HotkeysMapping.Add(Keys.End | Keys.Control | Keys.Shift, FCTBAction.GoLastLineWithSelection);
        fctb.HotkeysMapping.Add(Keys.Home | Keys.Control | Keys.Shift, FCTBAction.GoFirstLineWithSelection);
        fctb.HotkeysMapping.Add(Keys.Left | Keys.Control | Keys.Shift, FCTBAction.GoWordLeftWithSelection);
        fctb.HotkeysMapping.Add(Keys.Right | Keys.Control | Keys.Shift, FCTBAction.GoWordRightWithSelection);
        fctb.HotkeysMapping.Add(Keys.B | Keys.Control | Keys.Shift, FCTBAction.UnbookmarkLine);
        fctb.HotkeysMapping.Add(Keys.C | Keys.Control | Keys.Shift, FCTBAction.CommentSelected);
        fctb.HotkeysMapping.Add(Keys.N | Keys.Control | Keys.Shift, FCTBAction.GoPrevBookmark);
        fctb.HotkeysMapping.Add(Keys.U | Keys.Control | Keys.Shift, FCTBAction.LowerCase);
        fctb.HotkeysMapping.Add(Keys.OemMinus | Keys.Control | Keys.Shift, FCTBAction.NavigateForward);

        // --- ALT + Клавиши ---
        fctb.HotkeysMapping.Add(Keys.Back | Keys.Alt, FCTBAction.Undo); 

        fctb.HotkeysMapping.Add(Keys.Up | Keys.Alt, FCTBAction.MoveSelectedLinesUp);
        fctb.HotkeysMapping.Add(Keys.Down | Keys.Alt, FCTBAction.MoveSelectedLinesDown);
        fctb.HotkeysMapping.Add(Keys.F | Keys.Alt, FCTBAction.FindChar);

        // --- ALT + SHIFT (Колоночное выделение) ---
        fctb.HotkeysMapping.Add(Keys.Left | Keys.Alt | Keys.Shift, FCTBAction.GoLeft_ColumnSelectionMode);
        fctb.HotkeysMapping.Add(Keys.Up | Keys.Alt | Keys.Shift, FCTBAction.GoUp_ColumnSelectionMode);
        fctb.HotkeysMapping.Add(Keys.Right | Keys.Alt | Keys.Shift, FCTBAction.GoRight_ColumnSelectionMode);
        fctb.HotkeysMapping.Add(Keys.Down | Keys.Alt | Keys.Shift, FCTBAction.GoDown_ColumnSelectionMode);

        newTabPage.Controls.Add(fctb);
        tabControlEditor.TabPages.Add(newTabPage);
        tabControlEditor.SelectedTab = newTabPage;
        fctb.IsChanged = false;
        fctb.SelectionChanged += (s, ev) => {
            int line = fctb.Selection.Start.iLine + 1;
            lblStatus.Text = string.Format(Label.StatusLineFormat, line, fctb.LinesCount);
        };
        fctb.TextChanged += OnTextChanged;
        return fctb;
    }
    private FastColoredTextBox CurrentEditor => 
        tabControlEditor.SelectedTab?.Controls[0] as FastColoredTextBox;
    
    private void DgvErrors_MouseWheel(object sender, MouseEventArgs e)
    {
        if (Control.ModifierKeys == Keys.Control)
        {
            float currentSize = dgvErrors.DefaultCellStyle.Font.Size;
            float newSize = e.Delta > 0 ? currentSize + 1 : currentSize - 1;

            if (newSize >= 6 && newSize <= 32)
            {
                Font newFont = new Font(dgvErrors.Font.FontFamily, newSize);

                dgvErrors.DefaultCellStyle.Font = newFont;

                dgvErrors.ColumnHeadersDefaultCellStyle.Font = newFont;

                dgvErrors.AutoResizeColumnHeadersHeight();
            
                dgvErrors.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
            }

            ((HandledMouseEventArgs)e).Handled = true;
        }
    }
   private void ApplyLocalization()
    {
        // Заголовок окна
        this.Text = Label.AppTitle;
    
        // Меню Файл
        файлToolStripMenuItem.Text = Label.MenuFile;
        создатьToolStripMenuItem.Text = Label.MenuCreate;
        открытьToolStripMenuItem.Text = Label.MenuOpen;
        сохранитьToolStripMenuItem.Text = Label.MenuSave;
        сохранитьКакToolStripMenuItem.Text = Label.MenuSaveAs;
        выходToolStripMenuItem.Text = Label.MenuExit;

        // Меню Правка
        текстToolStripMenuItem.Text = Label.MenuEdit;
        отменитьToolStripMenuItem.Text = Label.MenuUndo;
        повторитToolStripMenuItem.Text = Label.MenuRedo;
        вырезатьToolStripMenuItem.Text = Label.MenuCut;
        копироватьToolStripMenuItem.Text = Label.MenuCopy;
        вставитьToolStripMenuItem.Text = Label.MenuPaste;
        удалитьToolStripMenuItem.Text = Label.MenuDelete;
        выделитьВсёToolStripMenuItem.Text = Label.MenuSelectAll;

        // Меню Текст
        текстToolStripMenuItem1.Text = Label.MenuText;
        постановкаЗадачиToolStripMenuItem.Text = Label.MenuTask;
        грамматикаToolStripMenuItem.Text = Label.MenuGrammar;
        классификацияГрамматикиToolStripMenuItem.Text = Label.MenuClass;
        методАнализаToolStripMenuItem.Text = Label.MenuMethod;
        тестовыйПримерToolStripMenuItem.Text = Label.MenuExample;
        списокЛитературыToolStripMenuItem.Text = Label.MenuDocs;
        исходныйКодПрограммыToolStripMenuItem.Text = Label.MenuSource;

        // Меню Пуск и Справка
        пускToolStripMenuItem.Text = Label.MenuRun;
        справкаToolStripMenuItem.Text = Label.MenuHelpRoot;
        вызовСправкиToolStripMenuItem.Text = Label.MenuShowHelp;
        оПрограммеToolStripMenuItem.Text = Label.MenuAbout;

        // Кнопки на панели (подсказки при наведении)
        toolStripButton1.Text = Label.MenuCreate;
        toolStripButton2.Text = Label.MenuOpen;
        toolStripButton3.Text = Label.MenuSave;
        toolStripButton10.Text = Label.MenuRun;
        toolStripButton11.Text = Label.MenuShowHelp;
        toolStripButton8.Text = Label.MenuAbout;

        // Таблица ошибок
        colFragment.HeaderText = Label.colFragment;
        colPos.HeaderText = Label.colPos;
        ColMessage.HeaderText = Label.ColMessage;
        // Кнопки на ToolStrip (текст всплывающих подсказок)
        toolStripButton4.Text = Label.MenuUndo;
        toolStripButton5.Text = Label.MenuRedo;
        toolStripButton6.Text = Label.MenuCopy;
        toolStripButton7.Text = Label.MenuCut;
        toolStripButton9.Text = Label.MenuPaste;

        // Меню выбора языка
        языкToolStripMenuItem.Text = Label.MenuLangRoot;
        русскийToolStripMenuItem.Text = Label.LangRu;
        английскийToolStripMenuItem.Text = Label.LangEn;
        
        // Начальный статус
        if (string.IsNullOrEmpty(lblStatus.Text)) 
            lblStatus.Text = Label.StatusReady;
    }
   
    private void englishToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeLanguage("en-US");
    }

    private void russianToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeLanguage("ru-RU");
    }
    private void ChangeLanguage(string cultureName)
    {
        System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(cultureName);
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(cultureName);

        ApplyLocalization();

        foreach (TabPage tab in tabControlEditor.TabPages)
        {
            if (tab.Tag == null || string.IsNullOrEmpty(tab.Tag.ToString()))
            {
                tab.Text = Label.NewFile; 
            }
        }
    }
    
    private void dgvErrors_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && dgvErrors.Rows[e.RowIndex].Tag is Token token)
        {
            if (CurrentEditor == null) return;

            int lineIndex = token.Line - 1;
            int charIndexInLine = token.StartPos - 1;

            try
            {
                FastColoredTextBoxNS.Place place = new FastColoredTextBoxNS.Place(charIndexInLine, lineIndex);
                CurrentEditor.Focus();
                CurrentEditor.SelectionStart = CurrentEditor.PlaceToPosition(place);
                CurrentEditor.SelectionLength = (token.EndPos - token.StartPos) + 1;
                CurrentEditor.DoSelectionVisible();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Ошибка навигации: " + ex.Message);
            }
        }
    }
    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        e.ChangedRange.ClearStyle(BlueStyle, BrownStyle);
        e.ChangedRange.SetStyle(BrownStyle, @"""""|@""[^""]*""|""[^""\\]*(?:\\.[^""\\]*)*""");
        e.ChangedRange.SetStyle(BlueStyle, @"\b(const|val)\b");
    }
    private void CloseTab_Click(object sender, EventArgs e)
    {
        CloseCurrentTab();
    }

    private void CloseCurrentTab()
    {
        if (tabControlEditor.SelectedTab == null) return;

        TabPage currentTab = tabControlEditor.SelectedTab;
        var fctb = currentTab.Controls[0] as FastColoredTextBox;

        if (fctb != null && fctb.IsChanged)
        {
            var result = MessageBox.Show(
                string.Format(Label.SaveConfirmMsg, currentTab.Text), 
                Label.ExitTitle, 
                MessageBoxButtons.YesNoCancel, 
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes) 
            {
                SaveFile(); 
                tabControlEditor.TabPages.Remove(currentTab);
            }
            else if (result == DialogResult.No) 
            {
                tabControlEditor.TabPages.Remove(currentTab);
            }
        }
        else
        {
            tabControlEditor.TabPages.Remove(currentTab);
        }
    }
}