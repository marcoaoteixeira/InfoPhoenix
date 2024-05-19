namespace Nameless.InfoPhoenix.Domain {
    /// <summary>
    /// This class was defined to be an entrypoint for this project assembly.
    /// 
    /// *** DO NOT IMPLEMENT ANYTHING HERE ***
    /// 
    /// But, it's allow to use this class as a repository for all constants or
    /// default values that we'll use throughout this project.
    /// </summary>
    public static class Root {
        #region Public Static Classes

        public static class Defaults {
            public const string TXT_EXTENSION = ".txt";
            public const string DOC_EXTENSION = ".doc";
            public const string DOCX_EXTENSION = ".docx";
            public const string RTF_EXTENSION = ".rtf";

            /// <summary>
            /// Gets the valid documents extensions: .doc, .docx, .rtf
            /// </summary>
            public static string[] ValidDocumentExtensions { get; } = [
                DOCX_EXTENSION,
                DOC_EXTENSION,
                RTF_EXTENSION,
                TXT_EXTENSION
            ];

            #region Public Static Inner Classes

            public static class Chars {
                #region Public Constants

                public const char WHITE_SPACE = ' ';
                public const char DASH = '-';

                #endregion
            }

            public static class Strings {
                #region Public Constants

                public const string DASH = "-";
                public const string DOUBLE_QUOTE_CHAR = "\"";
                public const string ASTERISK = "*";

                #endregion
            }

            #endregion
        }

        public static class DbErrors {
            #region Public Constants

            public const string DOCUMENT_DIRECTORY_NOT_FOUND = "Diretório de Documentos não foi localizado na base de dados.";
            public const string DOCUMENT_DIRECTORY_NOT_SAVED = "Não foi possível salvar as alterações no Diretório de Documentos.";
            public const string DOCUMENT_DIRECTORY_NOT_DELETED = "Não foi possível remover o Diretório de Documentos.";
            public const string NO_DOCUMENTS_TO_INDEX = "Não existem Documentos na Base de Dados marcados para indexação.";
            public const string COLLECT_DOCUMENTS_OPERATION_CANCELLED = "A coleta de Documentos foi cancelada pelo usuário.";

            #endregion
        }

        public static class Index {
            #region Public Constants

            public const string NAME = "00000000-0000-0000-0000-000000000000";
            public const string INDEX_OPERATION_CANCELLED = "A execução da indexação foi cancelada pelo usuário.";

            #endregion
        }

        #endregion
    }
}
