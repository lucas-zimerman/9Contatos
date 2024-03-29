﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using _9Contatos.Contatos.Carrega;
using _9Contatos.Contatos.Contato;
using _9Contatos.globais;

namespace _9Contatos.API.PeopleAPP
{
    /// <summary>
    /// Objeto utilizado para interagir com o App Pessoas diretamente
    /// </summary>
    class PeopleAPI
    {
        private List<Windows.ApplicationModel.Contacts.Contact> Contatos = new List<Windows.ApplicationModel.Contacts.Contact>();
        private ContactStore contactStore;

        /// <summary>
        ///  Função que altera um contato.
        /// </summary>
        /// <param name="Contato_a_Mudar"> O objeto contato que será alterado</param>
        /// <param name="NomeCompleto">O nome completo do contato a ser alterado (validação)</param>
        /// <param name="TelefonesNovos">Os números de telefone novos a serem alterados</param>
        /// <param name="TelefonesAntigos">Os números antigos de telefones (validação)</param>
        /// <returns>
        ///         0 = Feito
        ///         -1 = contagem de telefones antigos difere do número no contato_a_mudar
        ///         -2 o contagem de telefones novos difere do número no contato_a_mudar
        ///         -3 = Nome do contato se difere ao nomecompleto a ser validado
        /// </returns>
        public async Task<int> AlterarContato(Windows.ApplicationModel.Contacts.Contact Contato_a_Mudar, string NomeCompleto, List<string> TelefonesNovos, List<string> TelefonesAntigos)
        {
            int Valido = 0;
            contactStore = await Windows.ApplicationModel.Contacts.ContactManager.RequestStoreAsync(Windows.ApplicationModel.Contacts.ContactStoreAccessType.AllContactsReadWrite);

            var contactList = await contactStore.GetContactListAsync(Contato_a_Mudar.ContactListId);

            var contact = await contactList.GetContactAsync(Contato_a_Mudar.Id);
            if (contact.FullName != NomeCompleto)
            {
                Valido = -3;
            }
            else
            {
                if (contact.Phones.Count() != TelefonesAntigos.Count()) // não vamos alterar os números caso se o total de telefones for invalido
                {
                    Valido = -1;
                }
                else
                {
                    if (contact.Phones.Count() != TelefonesNovos.Count()) // não vamos alterar os números caso se o total de telefones for invalido
                    {
                        Valido = -2;
                    }
                    else
                    {
                        for (int i = 0, fim = TelefonesNovos.Count(); i < fim; i++)
                        {
                            contact.Phones[i].Number = TelefonesNovos[i];
                        }
                        await contactList.SaveContactAsync(contact);
                    }
                }
            }
            return Valido;
        }

        /// <summary>
        ///  Função que altera um contato do app, linkando ao contato que deveria ser editado
        /// </summary>
        /// <param name="Contato_a_Mudar"> O objeto contato que será alterado</param>
        /// <param name="NomeCompletos">O nome completo do contato a ser alterado (validação)</param>
        /// <param name="TelefonesNovos">Os números de telefone novos a serem alterados</param>
        /// <param name="TelefonesAntigos">Os números antigos de telefones (validação)</param>
        /// <returns>
        ///         0 = Feito
        ///         -1 = contagem de telefones antigos difere do número no contato_a_mudar
        ///         -2 o contagem de telefones novos difere do número no contato_a_mudar
        ///         -3 = Nome do contato se difere ao nomecompleto a ser validado
        /// </returns>
        public async Task<int> AlterarContato_Link(Contact Contato_a_Mudar, string NomeCompleto, List<string> TelefonesNovos, List<string> TelefonesAntigos)
        {
            Contact contato_temp;
            int Valido = 0;
            contactStore = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
            var listaListasApp = await contactStore.FindContactListsAsync();
            ContactList contactList;
            if (listaListasApp.Count() > 0)
            {
                contactList = listaListasApp.First();
                for (int i = 0, fim = TelefonesNovos.Count(); i < fim; i++)
                {
                    Contato_a_Mudar.Phones[i].Number = TelefonesNovos[i];
                }
                contato_temp = ClonaContato(ref Contato_a_Mudar);
                /*await*/
                contactList.SaveContactAsync(contato_temp);
            }
            else
            {
                Valido = -1;
            }
            return Valido;
        }


        /// <summary>
        ///     Carrega todos os contatos do Pessoas
        /// </summary>
        /// <returns></returns>
        public async Task<bool> LoadBuffer(QualAPI api)
        {
            //ContactStore allAccessStore;
            if (api == QualAPI.PeopleAPI_COM_Alteracao)
            {
                try
                {
                    contactStore = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AllContactsReadWrite);
                }
                catch(System.UnauthorizedAccessException)
                {
                    return false;
                }
            }
            else
            {
                contactStore = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
                if ((await contactStore.FindContactListsAsync()).Count == 0)
                {
                    await contactStore.CreateContactListAsync("Arruma Contatos");
                }
                else
                {
                    await Limpa_Contatos_Temporarios();
                    await contactStore.CreateContactListAsync("Arruma Contatos");
                }
            }
            Contatos.Clear();
            try
            {
                var contacts = await contactStore.FindContactsAsync(); // o try só serve para testar essa única linha
                // mas como o resto abaixo depende dele, vai ficar assim...
                foreach (var contact in contacts)
                {
                    //process aggregated contacts
                    if (contact.IsAggregate)
                    {
                        //here contact.ContactListId is "" (null....)                  
                        //in this case if you need the the ContactListId then you need to iterate through the raw contacts
                        if (api == QualAPI.PeopleAPI_COM_Alteracao)
                        {
                            var rawContacts = await contactStore.AggregateContactManager.FindRawContactsAsync(contact);
                            foreach (var rawContact in rawContacts)
                            {
                                Contatos.Add(rawContact);
                            }
                        }
                        else
                        {
                            //não precisamos do rawContacts
                            Contatos.Add(contact);
                        }
                    }
                    else //not aggregated contacts should work
                    {
                        //                    Debug.WriteLine($"not aggregated, name: {contact.DisplayName }, ContactListId: {contact.ContactListId}");
                    }
                }
            }
            catch (System.Exception)
            {
                //Po microsoft que Exception paia pra saber que o usuário marcou o aplicativo ou todos os aplicativos dele para não compartilhar os contatos...
                Globais.Contatos_Bloqueados_Pelo_User = true;
                //O erro vai ser tratado no CarregaContatos, depois do loop que espera essa task ser finalizada.
            }

            Globais.Contatos_Carregados = true;
            return true;
        }

        public Contato CarregaContato(int Indice)
        {
            Contato NovoContato = new Contato();
            if (Indice < Contatos.Count())
            {
                NovoContato.NomeCompleto = Contatos[Indice].FullName;
                for (int i = 0, fim = Contatos[Indice].Phones.Count(); i < fim; i++)
                {
                    NovoContato.Telefones_Antigos.Add(Contatos[Indice].Phones[i].Number);
                }
                NovoContato.ID = Contatos[Indice];
            }
            return NovoContato;
        }

        private Contact ClonaContato(ref Contact contato)
        {
            //Cortesia do código do Jaedson
            Contact cont = new Contact();
            foreach (var end in contato.Addresses)
            {
                cont.Addresses.Add(end);
            }
            cont.DisplayNameOverride = contato.DisplayNameOverride;
            cont.DisplayPictureUserUpdateTime = contato.DisplayPictureUserUpdateTime;
            foreach (var email in contato.Emails)
            {
                cont.Emails.Add(email);
            }
            foreach (var f in contato.Fields)
            {
                cont.Fields.Add(f);
            }
            cont.FirstName = contato.FirstName;
            cont.HonorificNamePrefix = contato.HonorificNamePrefix;
            cont.HonorificNameSuffix = contato.HonorificNameSuffix;
            foreach (var data in contato.ImportantDates)
            {
                cont.ImportantDates.Add(data);
            }
            cont.LastName = contato.LastName;
            cont.MiddleName = contato.MiddleName;
            cont.Name = contato.Name;
            cont.Nickname = contato.Nickname;
            cont.Notes = contato.Notes;

            foreach (var tel in contato.Phones)
            {   //número já foi alterado dentro do cont antes desta chamada
                cont.Phones.Add(tel);
            }
            foreach (var prop in contato.ProviderProperties)
            {
                cont.ProviderProperties.Add(prop);
            }
            cont.RingToneToken = contato.RingToneToken;
            foreach (var sig in contato.SignificantOthers)
            {
                cont.SignificantOthers.Add(sig);
            }
            cont.SourceDisplayPicture = contato.SourceDisplayPicture;
            cont.TextToneToken = contato.TextToneToken;
            cont.Thumbnail = contato.Thumbnail;
            foreach (var net in contato.Websites)
            {
                cont.Websites.Add(net);
            }
            cont.YomiFamilyName = contato.YomiFamilyName;
            cont.YomiGivenName = contato.YomiGivenName;

            return cont;
        }

        public int TotalContatos()
        {
            return Contatos.Count();
        }

        public async 
        Task
Limpa_Contatos_Temporarios()
        {
            //   var allAccessStore = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
            if (contactStore != null)
            {
                if ((await contactStore.FindContactListsAsync()).Count != 0)
                {
                    //Limpa contatos antigos
                    var listaListasApp = await contactStore.FindContactListsAsync();
                    if (listaListasApp.Count() == 0)
                    {
                        //não tem nada a ser deletado (parece bobera, mas o nosso amigo first ali em baixo tem tendencias de bugismo quando isso ocorre, então não vamos incomoda-lo
                    }
                    else
                    {
                        var lista = listaListasApp.First();
                        await lista.DeleteAsync();
                    }
                    //recria o link limpo
                }
            }
        }
    }
}
