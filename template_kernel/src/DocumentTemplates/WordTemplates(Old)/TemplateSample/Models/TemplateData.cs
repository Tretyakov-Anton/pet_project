using TemplatesCore;

namespace TemplateSample.Models
{
    public class TemplateData : ITemplateData
    {
        private string str;
        private int num;
        private bool bl;

        public string Str
        {
            get => str;
            private set => str = value;
        }
        public int Num
        {
            get => num;
            set => num = value;
        }
        public bool Bl
        {
            get => bl;
            private set => bl = value;
        }
        public TemplateData(string str, int num, bool bl)
        {
            this.str = str;
            this.num = num;
            this.bl = bl;
        }
    }
}
