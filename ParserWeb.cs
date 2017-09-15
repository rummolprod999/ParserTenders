namespace ParserTenders
{
    public class ParserWeb : IParserWeb
    {
        protected TypeArguments ar;

        public ParserWeb(TypeArguments ar)
        {
            this.ar = ar;
        }

        public virtual void Parsing()
        {
        }
    }
}