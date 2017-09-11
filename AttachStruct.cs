namespace ParserTenders
{
    public struct AttachStruct
    {
        public int IdAttach;
        public string UrlAttach;
        public TypeFileAttach TypeF;


        public AttachStruct(int idAttach, string urlAttach, TypeFileAttach typeF)
        {
            IdAttach = idAttach;
            UrlAttach = urlAttach;
            TypeF = typeF;
        }
    }
}