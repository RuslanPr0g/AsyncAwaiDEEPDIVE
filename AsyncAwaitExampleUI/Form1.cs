namespace AsyncAwaitExampleUI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            label1.Text = BigLongImportantMethod("User");
        }

        private string BigLongImportantMethod(string name)
        {
            Thread.Sleep(2000);
            return $"Hello, {name}";
        }
    }
}