using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PythonCompiler
{
    public partial class Form1 : Form
    {
        private RichTextBox txtCode;
        private ComboBox cmbViewSelector;
        private DataGridView dgvTokens;
        private TreeView tvAst;
        private Button btnCompile;
        private SplitContainer splitContainer;
        private Panel rightPanel;

        public Form1()
        {
            InitializeComponent();
            SetupDataGridView();
        }

        private void InitializeComponent()
        {
            this.Text = "Compilador de Python - Fases";
            this.Size = new Size(1000, 650);

            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 450
            };
            
            txtCode = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 12f),
                AcceptsTab = true,
                Text = "def calcular_suma(a, b):\n    resultado = a + b\n    return resultado\n\nif True:\n    print(calcular_suma(10, 5))"
            };

            rightPanel = new Panel { Dock = DockStyle.Fill };

            cmbViewSelector = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 11f),
                Height = 30
            };
            cmbViewSelector.Items.Add("1. Analizador Léxico (Tabla de Tokens)");
            cmbViewSelector.Items.Add("2. Analizador Sintáctico (Árbol AST)");
            cmbViewSelector.SelectedIndex = 0;
            cmbViewSelector.SelectedIndexChanged += CmbViewSelector_SelectedIndexChanged;

            dgvTokens = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            tvAst = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 11f),
                Visible = false 
            };

            rightPanel.Controls.Add(dgvTokens);
            rightPanel.Controls.Add(tvAst);
            rightPanel.Controls.Add(cmbViewSelector);

            btnCompile = new Button
            {
                Text = "Analizar Código",
                Dock = DockStyle.Bottom,
                Height = 50,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.LightGray
            };
            btnCompile.Click += BtnCompile_Click;

            splitContainer.Panel1.Controls.Add(txtCode);
            splitContainer.Panel2.Controls.Add(rightPanel);

            this.Controls.Add(splitContainer);
            this.Controls.Add(btnCompile);
        }

        private void SetupDataGridView()
        {
            dgvTokens.Columns.Clear();
            dgvTokens.Columns.Add("Type", "Tipo de Token");
            dgvTokens.Columns.Add("Value", "Valor");
            dgvTokens.Columns.Add("Line", "Línea");
            dgvTokens.Columns.Add("Column", "Columna");
        }

        private void CmbViewSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbViewSelector.SelectedIndex == 0)
            {
                dgvTokens.Visible = true;
                tvAst.Visible = false;
            }
            else
            {
                dgvTokens.Visible = false;
                tvAst.Visible = true;
            }
        }

        private void BtnCompile_Click(object sender, EventArgs e)
        {
            dgvTokens.Rows.Clear();
            tvAst.Nodes.Clear();
            string sourceCode = txtCode.Text;

            try
            {
                Tokenizer lexer = new Tokenizer(sourceCode);
                List<Token> tokens = lexer.Tokenize();

                foreach (var token in tokens)
                {
                    string displayValue = token.Type == TokenType.NewLine ? "\\n" : token.Value;
                    dgvTokens.Rows.Add(token.Type.ToString(), displayValue, token.Line, token.Column);
                }

                Parser parser = new Parser(tokens);
                ProgramNode programAst = parser.Parse();

                TreeNode rootNode = new TreeNode("Programa Python");
                foreach (var stmt in programAst.Statements)
                {
                    rootNode.Nodes.Add(BuildAstTree(stmt));
                }
                
                tvAst.Nodes.Add(rootNode);
                tvAst.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error de Compilación", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private TreeNode BuildAstTree(AstNode node)
        {
            // NUEVO: Pasa la expresión interna directamente al árbol
            if (node is ExpressionStatementNode exprStmt)
            {
                return BuildAstTree(exprStmt.Expression);
            }

            if (node is AssignmentNode assignNode)
            {
                TreeNode tn = new TreeNode($"Asignación (=)");
                tn.Nodes.Add(new TreeNode($"Variable: {assignNode.VariableName}"));
                TreeNode valueNode = new TreeNode("Valor");
                valueNode.Nodes.Add(BuildAstTree(assignNode.Value));
                tn.Nodes.Add(valueNode);
                return tn;
            }
            
            if (node is IfStatementNode ifNode)
            {
                TreeNode tn = new TreeNode("Condicional (if)");
                
                TreeNode condNode = new TreeNode("Condición");
                condNode.Nodes.Add(BuildAstTree(ifNode.Condition));
                tn.Nodes.Add(condNode);

                TreeNode thenNode = new TreeNode("Bloque True (INDENT)");
                foreach (var stmt in ifNode.ThenBranch) thenNode.Nodes.Add(BuildAstTree(stmt));
                tn.Nodes.Add(thenNode);

                if (ifNode.ElseBranch.Count > 0)
                {
                    TreeNode elseNode = new TreeNode("Bloque False (else)");
                    foreach (var stmt in ifNode.ElseBranch) elseNode.Nodes.Add(BuildAstTree(stmt));
                    tn.Nodes.Add(elseNode);
                }
                return tn;
            }

            if (node is FunctionDefinitionNode funcDefNode)
            {
                TreeNode tn = new TreeNode($"Definición de Función: def {funcDefNode.Name}()");
                
                if (funcDefNode.Parameters.Count > 0)
                {
                    TreeNode paramsNode = new TreeNode($"Parámetros: {string.Join(", ", funcDefNode.Parameters)}");
                    tn.Nodes.Add(paramsNode);
                }

                TreeNode bodyNode = new TreeNode("Cuerpo (INDENT)");
                foreach (var stmt in funcDefNode.Body)
                {
                    bodyNode.Nodes.Add(BuildAstTree(stmt));
                }
                tn.Nodes.Add(bodyNode);
                
                return tn;
            }

            if (node is WhileStatementNode whileNode)
            {
                TreeNode tn = new TreeNode("Bucle (while)");
                TreeNode condNode = new TreeNode("Condición");
                condNode.Nodes.Add(BuildAstTree(whileNode.Condition));
                tn.Nodes.Add(condNode);

                TreeNode bodyNode = new TreeNode("Cuerpo (INDENT)");
                foreach (var stmt in whileNode.Body) bodyNode.Nodes.Add(BuildAstTree(stmt));
                tn.Nodes.Add(bodyNode);
                return tn;
            }

            if (node is ForStatementNode forNode)
            {
                TreeNode tn = new TreeNode($"Bucle (for {forNode.IteratorVariable} in ...)");
                TreeNode iterNode = new TreeNode("Iterable");
                iterNode.Nodes.Add(BuildAstTree(forNode.Iterable));
                tn.Nodes.Add(iterNode);

                TreeNode bodyNode = new TreeNode("Cuerpo (INDENT)");
                foreach (var stmt in forNode.Body) bodyNode.Nodes.Add(BuildAstTree(stmt));
                tn.Nodes.Add(bodyNode);
                return tn;
            }

            if (node is ReturnStatementNode retNode)
            {
                TreeNode tn = new TreeNode("Retorno (return)");
                if (retNode.Value != null)
                {
                    tn.Nodes.Add(BuildAstTree(retNode.Value));
                }
                return tn;
            }

            if (node is FunctionCallNode funcNode)
            {
                TreeNode tn = new TreeNode($"Llamada a función: {funcNode.FunctionName}()");
                if (funcNode.Arguments.Count > 0)
                {
                    TreeNode argsNode = new TreeNode("Argumentos");
                    foreach (var arg in funcNode.Arguments) argsNode.Nodes.Add(BuildAstTree(arg));
                    tn.Nodes.Add(argsNode);
                }
                return tn;
            }

            if (node is BinaryOpNode binNode)
            {
                TreeNode tn = new TreeNode($"Operación: {binNode.Operator}");
                tn.Nodes.Add(BuildAstTree(binNode.Left));
                tn.Nodes.Add(BuildAstTree(binNode.Right));
                return tn;
            }

            if (node is NumberLiteralNode numNode) return new TreeNode($"Número: {numNode.Value}");
            if (node is StringLiteralNode strNode) return new TreeNode($"String: {strNode.Value}");
            if (node is VariableAccessNode varNode) return new TreeNode($"Variable: {varNode.Name}");
            if (node is BooleanLiteralNode boolNode) return new TreeNode($"Booleano: {boolNode.Value}");
            if (node is NoneLiteralNode) return new TreeNode("Nulo: None");

            return new TreeNode($"Nodo desconocido: {node.GetType().Name}");
        }
    }
}