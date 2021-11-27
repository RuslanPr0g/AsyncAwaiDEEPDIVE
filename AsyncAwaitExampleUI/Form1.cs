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

        private async void button1_Click(object sender, EventArgs e)
        {
            var operation = Task.Factory.StartNew(() => BigLongImportantMethod("User"));

            label1.Text = await operation;
        }

        private string BigLongImportantMethod(string name)
        {
            Thread.Sleep(2000);
            return $"Hello, {name}";
        }
    }
}