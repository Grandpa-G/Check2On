using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
namespace Check2On
{
    class Config
    {
        public int action = 0;
        public bool email = false;
        public string host = "";
        public int port = 25;
        public string credentialsUser = "";
        public string credentialsPassword = "";
        public string sendTo = "";
        public string sendFrom = "";
        public bool EnableSsl = false;

        public void Write(string path)
		{
			File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
		}

		public static Config Read(string path)
		{
			return !File.Exists(path)
				? new Config()
				: JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
		}
	}
}

