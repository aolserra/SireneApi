using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SireneAPI
{
    public static class Constants
    {
        public static readonly string DbUrl = "https://sirenetst-default-rtdb.firebaseio.com/";

        public static readonly string IdQuery = "ID";
        public static readonly string NameQuery = "NAME";
        public static readonly string ConnectionIdQuery = "CONNECTIONID";
        public static readonly string IdAccessQuery = "IDACCESS";
        public static readonly string GroupNameQuery = "GROUPNAME";
        public static readonly string PartialUserCodeQuery = "PARTIALUSERCODE";
        public static readonly string NotifMsgGroupQuery = "GROUP";
        public static readonly string IdComunidadeQuery = "IDCOMUNIDADE";
        public static readonly string IdComunidadePerfilQuery = "IDCOMUNIDADE&PERFIL";

        public static readonly string TabGrupo = "Grupos";
        public static readonly string TabNotificacao = "Notificacoes";
        public static readonly string TabUsuario = "Usuarios";
    }
}
