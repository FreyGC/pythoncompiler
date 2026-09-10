using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PythonCompiler
{
    public partial class Form1 : Form
    {
        private RichTextBox txtCode;
        private DataGridView dgvTokens;
        private Button btnTokenize;
        private SplitContainer splitContainer;

        public Form1()
        {
            InitializeComponent();
            SetupDataGridView();
        }

        // Aquí creamos los elementos visuales manualmente
        private void InitializeComponent()
        {
            this.Text = "Tokenizador de Python";
            this.Size = new Size(900, 600);

            // SplitContainer divide la pantalla en dos mitades ajustables
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 450
            };
            
            // Caja de texto para el código
            txtCode = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 12f),
                AcceptsTab = true,
                Text = "def evaluar_numero(num):\n    if num > 10:\n        print(\"Mayor\")\n    return True"
            };

            // Tabla de tokens
            dgvTokens = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Botón en la parte inferior
            btnTokenize = new Button
            {
                Text = "Generar Tokens",
                Dock = DockStyle.Bottom,
                Height = 50,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.LightGray
            };
            btnTokenize.Click += new EventHandler(btnTokenize_Click);

            // Ensamblar todo en la ventana
            splitContainer.Panel1.Controls.Add(txtCode);
            splitContainer.Panel2.Controls.Add(dgvTokens);

            this.Controls.Add(splitContainer);
            this.Controls.Add(btnTokenize);
        }

        private void SetupDataGridView()
        {
            dgvTokens.Columns.Clear();
            dgvTokens.Columns.Add("Type", "Tipo de Token");
            dgvTokens.Columns.Add("Value", "Valor");
            dgvTokens.Columns.Add("Line", "Línea");
            dgvTokens.Columns.Add("Column", "Columna");
        }

        private void btnTokenize_Click(object sender, EventArgs e)
        {
            dgvTokens.Rows.Clear();
            string sourceCode = txtCode.Text;

            Tokenizer lexer = new Tokenizer(sourceCode);
            List<Token> tokens = lexer.Tokenize();

            foreach (var token in tokens)
            {
                string displayValue = token.Type == TokenType.NewLine ? "\\n" : token.Value;
                dgvTokens.Rows.Add(token.Type.ToString(), displayValue, token.Line, token.Column);
            }
        }
    }
}