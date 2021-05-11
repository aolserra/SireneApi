using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SireneAPI.Services
{
    public class FirebaseService
    {
        public FirebaseClient client;

        public FirebaseService()
        {
            client = new FirebaseClient(Constants.DbUrl);
        }
    }
}
