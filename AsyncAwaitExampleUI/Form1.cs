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
            _ = CallImportantMethod("User");
        }

        private async Task CallImportantMethod(string name)
        {
            label1.Text = await BigLongImportantMethodAsync(name);
        }

        private Task<string> BigLongImportantMethodAsync(string name)
        {
            return Task.Factory.StartNew(() => BigLongImportantMethod(name));
        }

        private string BigLongImportantMethod(string name)
        {
            Thread.Sleep(2000);
            return $"Hello, {name}";
        }
    }
}