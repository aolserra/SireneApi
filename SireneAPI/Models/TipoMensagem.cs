using System;

namespace SireneAPI.Models
{
	public class TipoMensagem
	{
		public string Id { get; set; }

		public string MsgIdComunidade { get; set; }

		public string MsgComunidade { get; set; }

		public string MsgTipo { get; set; }

		public string MsgTexto { get; set; }

		public DateTime MsgDataCriacao { get; set; }

		public DateTime MsgDataAlteracao { get; set; }

		public string MsgLogAlteracao { get; set; }
	}
}