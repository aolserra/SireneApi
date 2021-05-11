using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using SireneAPI.Models;
using SireneAPI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SireneAPI.Hubs
{
    public class SireneHub : Hub
    {
        public SireneHub()
        {
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Usuario user = await UsersService.GetInstance().GetUser(Constants.ConnectionIdQuery, Context.ConnectionId);
            if (user != null)
            {
                await DelUserConnectionId(user);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task Login(Usuario user)
        {
            Usuario userDB = await UsersService.GetInstance().GetUser(Constants.IdQuery, user.Id);

            if (userDB == null)
            {
                await Clients.Caller.SendAsync("ReceiveLogin", false, null);
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveLogin", true, userDB);

                userDB.IsOnline = true;

                await UsersService.GetInstance().UpdateUser(userDB);
            }
        }

        public async Task Logout(Usuario user)
        {
            Usuario userDB = await UsersService.GetInstance().GetUser(Constants.IdQuery, user.Id);

            if (userDB == null)
            {
                await Clients.Caller.SendAsync("ReceiveLogout", false, null, "Usuário não localizado no Banco de Dados!");
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveLogout", true, userDB, null);

                userDB.IsOnline = false;

                await UsersService.GetInstance().UpdateUser(userDB);

                await DelUserConnectionId(userDB);
            }
        }

        public async Task AddUserConnectionId(Usuario user)
        {
            try
            {
                Usuario userDB = await UsersService.GetInstance().GetUser(Constants.IdQuery, user.Id);

                if (userDB == null)
                {
                    await Clients.Caller.SendAsync("ReceiveAddUserConnectionId", false, "Usuário não localizado no banco de dados.");
                }
                else
                {
                    var currentConnectionId = Context.ConnectionId;
                    List<string> connectionsId;

                    if (string.IsNullOrEmpty(userDB.ConnectionID))
                    {
                        connectionsId = new List<string>
                        {
                            currentConnectionId
                        };
                    }
                    else
                    {
                        connectionsId = JsonConvert.DeserializeObject<List<string>>(userDB.ConnectionID);

                        if (!connectionsId.Contains(currentConnectionId))
                        {
                            connectionsId.Add(currentConnectionId);
                        }
                    }

                    userDB.IsOnline = true;
                    userDB.ConnectionID = JsonConvert.SerializeObject(connectionsId);

                    await UsersService.GetInstance().UpdateUser(userDB);

                    //Adicionar ConnectionsIds aos grupos de conversa do SignalR
                    string groupName = userDB.IdComunidadeUsu;
                    var groups = await GrupoService.GetInstance().GetGroupList(Constants.NameQuery, groupName);
                    foreach (var connectionId in connectionsId)
                    {
                        foreach (var group in groups)
                        {
                            await Groups.AddToGroupAsync(connectionId, group.Nome);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                StringBuilder msgSb = new();
                msgSb.Append("O documento não pode ser salvo!");
                msgSb.Append(Environment.NewLine);
                msgSb.Append("Código do Erro: ");
                msgSb.Append(e.ToString());
                await Clients.Caller.SendAsync("ReceiveAddUserConnectionId", false, msgSb.ToString());
            }
        }

        public async Task DelUserConnectionId(Usuario user)
        {
            Usuario userDB = await UsersService.GetInstance().GetUser(Constants.IdQuery, user.Id);
            if (userDB == null)
            {
                //await Clients.Caller.SendAsync("DelUserConnectionId", false, null, "Usuário não localizado no Banco de Dados!");
            }
            else
            {
                List<string> connectionsId = null;
                if (!string.IsNullOrEmpty(userDB.ConnectionID))
                {

                    var currentConnectionId = Context.ConnectionId;

                    connectionsId = JsonConvert.DeserializeObject<List<string>>(userDB.ConnectionID);
                    if (connectionsId.Contains(currentConnectionId))
                    {
                        connectionsId.Remove(currentConnectionId);
                    }
                    userDB.ConnectionID = JsonConvert.SerializeObject(connectionsId);
                }

                if (connectionsId.Count <= 0)
                {
                    userDB.IsOnline = false;
                }

                await UsersService.GetInstance().UpdateUser(userDB);

                //Remover ConnectrionsIds dos grupos de conversa do SignalR
                string groupName = userDB.IdComunidadeUsu;
                var groups = await GrupoService.GetInstance().GetGroupList(Constants.NameQuery, groupName);
                foreach (var connectionId in connectionsId)
                {
                    foreach (var group in groups)
                    {
                        await Groups.RemoveFromGroupAsync(connectionId, group.Nome);
                    }
                }
            }
        }

        public async Task CreateOrOpenGroup(Usuario currentUser)
        {
            try
            {
                bool isNewGrp;
                var group = await GrupoService.GetInstance().GetGroup(currentUser.IdComunidadeUsu);
                if (group == null)
                {
                    isNewGrp = true;

                    //Se não encontrou o grupo no banco de dados, cria um.
                    group = new Grupo
                    {
                        Id = Guid.NewGuid().ToString(),
                        Nome = currentUser.IdComunidadeUsu,
                    };
                }
                else
                {
                    isNewGrp = false;
                    group.Usuarios = null;
                }

                IList<Usuario> listUsers = null;
                listUsers = await UsersService.GetInstance().GetUsersList(Constants.IdComunidadeQuery, group.Nome);

                List<string> listUserId = new();
                foreach (Usuario user in listUsers)
                {
                    listUserId.Add(user.Id);
                    //Adicionando os Connections Id's dos usuários no grupo do SignalR
                    if (!string.IsNullOrEmpty(user.ConnectionID))
                    {
                        var connectionsId = JsonConvert.DeserializeObject<List<string>>(user.ConnectionID);
                        foreach (var connectionId in connectionsId)
                        {
                            await Groups.AddToGroupAsync(connectionId, group.Nome);
                        }
                    }
                }

                if (!listUserId.Contains(currentUser.Id))
                {
                    listUserId.Add(currentUser.Id);
                }

                //Salvando os Id's dos usuários no grupo do Banco de dados.
                group.Usuarios = JsonConvert.SerializeObject(listUserId);
                if (isNewGrp)
                {
                    var success = await GrupoService.GetInstance().AddGroup(group);
                }
                else
                {
                    await GrupoService.GetInstance().UpdateGroup(group);
                }

                await Clients.Caller.SendAsync("OpenGroup", group.Nome);
            }
            catch (Exception e)
            {
                StringBuilder sbError = new();
                sbError.Append("Erro: ");
                sbError.Append(e.ToString());
                await Clients.Caller.SendAsync("OpenGroup", "", sbError);
            }
        }

        public async Task SendMessage(Usuario user, string msgType, string msg, string groupName)
        {
            try
            {
                Grupo group = await GrupoService.GetInstance().GetGroup(groupName);
                if (!group.Usuarios.Contains(user.Id))
                {
                    StringBuilder sbMsg = new();
                    sbMsg.Append("O usuário ");
                    sbMsg.Append(user.NomeUsuario);
                    sbMsg.Append(" não pertence ao grupo ");
                    sbMsg.Append(group.Nome);
                    await Clients.Caller.SendAsync("ReceiveMessage", null, group, sbMsg.ToString());
                }

                Notificacao message = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    GroupName = groupName,
                    UserId = user.Id,
                    UserJason = JsonConvert.SerializeObject(user),
                    User = user,
                    MsgType = msgType,
                    Text = msg,
                    Completed = false,
                    CreatedDate = DateTime.Now
                };

                var success = NotifMsgService.GetInstance().AddNotifMsg(message);
                await Clients.Group(groupName).SendAsync("ReceiveMessage", message, group, user.Id);
                await Clients.Caller.SendAsync("ReceiveMessage", message, group, user.Id);
            }
            catch (Exception e)
            {
                StringBuilder sbError = new();
                sbError.Append("Erro: ");
                sbError.Append(e.ToString());
                await Clients.Caller.SendAsync("ReceiveMessage", null, null, sbError.ToString());
            }
        }

        public async Task MonitoringNotification(Notificacao msg)
        {
            try
            {
                List<Monitoring> monitoringList = new();
                Grupo group = await GrupoService.GetInstance().GetGroup(msg.GroupName);

                List<String> msgRecipients = JsonConvert.DeserializeObject<List<string>>(group.Usuarios);
                foreach (string userId in msgRecipients)
                {
                    Usuario user = await UsersService.GetInstance().GetUser(Constants.IdQuery, userId);
                    Monitoring monitoring;
                    if (Context.ConnectionId != user.ConnectionID)
                    {
                        monitoring = new Monitoring
                        {
                            Id = user.Id,
                            Nome = user.NomeUsuario,
                            IsOnline = user.IsOnline,
                            Resposta = user.RespostaNotifUsu,
                            TemPessoasComProbMobilidade = user.TemPessoasComProbMobilidade,
                        };
                        monitoringList.Add(monitoring);
                    }
                }

                await Clients.Caller.SendAsync("ReceiveMonitoringNotification", monitoringList, null);
            }
            catch (Exception e)
            {
                StringBuilder sbError = new();
                sbError.Append("Erro: ");
                sbError.Append(e.ToString());
                await Clients.Caller.SendAsync("ReceiveMonitoringNotification", null, sbError.ToString());
            }
        }

        public async Task ReplyNotification(Usuario user, string groupName, string response)
        {
            try
            {
                user.RespostaNotifUsu = response;
                await UsersService.GetInstance().UpdateUser(user);

                Monitoring monitoring = new()
                {
                    Id = user.Id,
                    Nome = user.NomeUsuario,
                    IsOnline = user.IsOnline,
                    Resposta = response,
                    TemPessoasComProbMobilidade = user.TemPessoasComProbMobilidade,
                };

                await Clients.All.SendAsync("ReceiveReplayNotification", monitoring, null);
            }
            catch (Exception e)
            {
                StringBuilder sbError = new();
                sbError.Append("Erro: ");
                sbError.Append(e.ToString());
                await Clients.Group(groupName).SendAsync("ReceiveReplayNotification", null, sbError.ToString());
            }
        }

        public async Task FinalizeNotification(string userQueryKey, Usuario currentUser)
        {
            try
            {
                var msgList = await NotifMsgService.GetInstance().GetNotifMsgList(Constants.GroupNameQuery, currentUser.IdComunidadeUsu);
                if (msgList.Any())
                {
                    foreach (Notificacao msg in msgList)
                    {
                        if (msg.Completed != true)
                        {
                            IList<Usuario> listUser = null;
                            listUser = await UsersService.GetInstance().GetUsersList(Constants.IdComunidadeQuery, currentUser.IdComunidadeUsu);

                            var listUserDB = listUser;
                            if (listUserDB.Any())
                            {
                                foreach (Usuario userDB in listUserDB)
                                {
                                    userDB.RespostaNotifUsu = null;
                                    await UsersService.GetInstance().UpdateUser(userDB);
                                }
                            }
                            else
                            {
                                StringBuilder sbError = new();
                                sbError.Append("List<UserRegistrationItem> Not Found!");
                                sbError.Append(Environment.NewLine);
                                sbError.Append("queryKey: ");
                                sbError.Append(userQueryKey);
                                sbError.Append(Environment.NewLine);
                                sbError.Append("groupName: ");
                                sbError.Append(currentUser.IdComunidadeUsu);

                                await Clients.Caller.SendAsync("ReceiveFinalization", sbError.ToString());
                            }

                            msg.Completed = true;
                            await NotifMsgService.GetInstance().UpdateNotifMsg(msg);
                            await Clients.Caller.SendAsync("ReceiveFinalization", null);
                        }
                    }
                }
                else
                {
                    StringBuilder sbError = new();
                    sbError.Append("NotificationsMessagesItem Not Found: ");
                    sbError.Append(currentUser.IdComunidadeUsu);
                    await Clients.Caller.SendAsync("ReceiveFinalization", sbError.ToString());
                }
            }
            catch (Exception e)
            {
                StringBuilder sbError = new();
                sbError.Append("Erro: ");
                sbError.Append(e.ToString());
                await Clients.Caller.SendAsync("ReceiveFinalization", sbError.ToString());
            }
        }

        //public async Task StartBackground()
        //{
        //    await Clients.Caller.SendAsync("BackgroundStarted", "Started");
        //}
    }
}
