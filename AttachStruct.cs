namespace ParserTenders
{
    public struct AttachStruct
    {
        public int id_attach;
        public string url_attach;
        public TypeFileAttach type_f;


        public AttachStruct(int idAttach, string urlAttach, TypeFileAttach typeF)
        {
            id_attach = idAttach;
            url_attach = urlAttach;
            type_f = typeF;
        }
    }
}