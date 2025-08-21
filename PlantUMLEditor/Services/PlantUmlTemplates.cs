using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace PlantUMLEditor.Services
{
    internal class PlantUmlTemplates
    {
        public static Dictionary<string, string> Templates { get; private set; } = new Dictionary<string, string>();

        private static readonly string _templatesFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PlantUMLEditor",
            "templates.json");

        private static readonly Dictionary<string, string> _defaultTemplates = new Dictionary<string, string>
        {
            { "Dijagram slijeda",
              "@startuml\n" +
              "title Dijagram slijeda\n\n" +
              "actor Korisnik\n" +
              "participant \"Prva komponenta\" as A\n" +
              "participant \"Druga komponenta\" as B\n\n" +
              "Korisnik -> A: Zahtjev\n" +
              "A -> B: Prosljedivanje zahtjeva\n" +
              "B --> A: Odgovor\n" +
              "A --> Korisnik: Prikaz rezultata\n" +
              "@enduml"
            },

            { "Dijagram slučajeva korištenja",
              "@startuml\n" +
              "title Dijagram slučajeva korištenja\n\n" +
              "left to right direction\n" +
              "actor Korisnik\n" +
              "actor Administrator\n\n" +
              "rectangle Sustav {\n" +
              "  Korisnik -- (Prijava)\n" +
              "  Korisnik -- (Pregled podataka)\n" +
              "  (Upravljanje korisnicima) -- Administrator\n" +
              "  (Konfiguracija sustava) -- Administrator\n" +
              "}\n" +
              "@enduml"
            },

            { "Dijagram klasa",
              "@startuml\n" +
              "title Dijagram klasa\n\n" +
              "class Korisnik {\n" +
              "  -String ime\n" +
              "  -String prezime\n" +
              "  +login()\n" +
              "  +logout()\n" +
              "}\n\n" +
              "class Proizvod {\n" +
              "  -String naziv\n" +
              "  -double cijena\n" +
              "  +getCijena()\n" +
              "}\n\n" +
              "Korisnik \"1\" -- \"*\" Proizvod: kupuje >\n" +
              "@enduml"
            },

            { "Objektni dijagram",
              "@startuml\n" +
              "title Objektni dijagram\n\n" +
              "object Korisnik1 {\n" +
              "  ime = \"Ivan\"\n" +
              "  prezime = \"Horvat\"\n" +
              "}\n\n" +
              "object Proizvod1 {\n" +
              "  naziv = \"Laptop\"\n" +
              "  cijena = 5999.99\n" +
              "}\n\n" +
              "Korisnik1 --> Proizvod1\n" +
              "@enduml"
            },

            { "Dijagram aktivnosti",
              "@startuml\n" +
              "title Dijagram aktivnosti\n\n" +
              "start\n" +
              ":Prijava korisnika;\n" +
              "if (Ispravni podaci?) then (da)\n" +
              "  :Pristup sustavu;\n" +
              "else (ne)\n" +
              "  :Prikaz greske;\n" +
              "  stop\n" +
              "endif\n" +
              ":Rad u sustavu;\n" +
              ":Odjava;\n" +
              "stop\n" +
              "@enduml"
            },

            { "Komponentni dijagram",
              "@startuml\n" +
              "title Komponentni dijagram\n\n" +
              "package \"Prezentacijski sloj\" {\n" +
              "  [Web aplikacija]\n" +
              "  [Mobilna aplikacija]\n" +
              "}\n\n" +
              "package \"Poslovni sloj\" {\n" +
              "  [Poslovna logika]\n" +
              "  [Servisi]\n" +
              "}\n\n" +
              "package \"Podatkovni sloj\" {\n" +
              "  [Baza podataka]\n" +
              "}\n\n" +
              "[Web aplikacija] --> [Poslovna logika]\n" +
              "[Mobilna aplikacija] --> [Poslovna logika]\n" +
              "[Poslovna logika] --> [Servisi]\n" +
              "[Servisi] --> [Baza podataka]\n" +
              "@enduml"
            },

            { "Dijagram raspoređivanja",
              "@startuml\n" +
              "title Dijagram rasporedivanja\n\n" +
              "node \"Web server\" {\n" +
              "  [Web aplikacija]\n" +
              "}\n\n" +
              "node \"Aplikacijski server\" {\n" +
              "  [Poslovna logika]\n" +
              "  [Servisi]\n" +
              "}\n\n" +
              "database \"Baza podataka\" {\n" +
              "  [Podaci]\n" +
              "}\n\n" +
              "[Web aplikacija] --> [Poslovna logika]\n" +
              "[Poslovna logika] --> [Servisi]\n" +
              "[Servisi] --> [Podaci]\n" +
              "@enduml"
            },

            { "Dijagram stanja",
              "@startuml\n" +
              "title Dijagram stanja\n\n" +
              "scale 350 width\n\n" +
              "[*] --> Neaktivan\n\n" +
              "Neaktivan --> Aktivan : Aktivacija\n" +
              "Aktivan --> Neaktivan : Deaktivacija\n" +
              "Aktivan --> Zakljucan : Zakljucavanje\n" +
              "Zakljucan --> Aktivan : Otkljucavanje\n" +
              "Zakljucan --> Neaktivan : Deaktivacija\n\n" +
              "Aktivan --> [*] : Brisanje\n" +
              "Neaktivan --> [*] : Brisanje\n" +
              "@enduml"
            },

            { "Mentalna mapa",
              "@startmindmap\n" +
              "title Mentalna mapa\n\n" +
              "* Projekt\n" +
              "** Analiza\n" +
              "*** Zahtjevi\n" +
              "*** Specifikacija\n" +
              "** Razvoj\n" +
              "*** Frontend\n" +
              "*** Backend\n" +
              "*** Baza podataka\n" +
              "** Testiranje\n" +
              "*** Jedinicni testovi\n" +
              "*** Integracijski testovi\n" +
              "** Implementacija\n" +
              "@endmindmap"
            },

            { "Ganttov dijagram",
              "@startgantt\n" +
              "title Ganttov dijagram\n\n" +
              "Project starts 2025-04-20\n\n" +
              "[Analiza] lasts 10 days\n" +
              "[Dizajn] lasts 15 days\n" +
              "[Implementacija] lasts 25 days\n" +
              "[Testiranje] lasts 10 days\n\n" +
              "[Dizajn] starts at [Analiza]'s end\n" +
              "[Implementacija] starts at [Dizajn]'s end\n" +
              "[Testiranje] starts at [Implementacija]'s end\n" +
              "@endgantt"
            },

            { "ER dijagram",
              "@startuml\n" +
              "title ER dijagram\n\n" +
              "entity Korisnik {\n" +
              "  * id : number <<generated>>\n" +
              "  --\n" +
              "  * ime : text\n" +
              "  * prezime : text\n" +
              "  email : text\n" +
              "}\n\n" +
              "entity Proizvod {\n" +
              "  * id : number <<generated>>\n" +
              "  --\n" +
              "  * naziv : text\n" +
              "  * cijena : number\n" +
              "  opis : text\n" +
              "}\n\n" +
              "entity Narudzba {\n" +
              "  * id : number <<generated>>\n" +
              "  --\n" +
              "  * datum : date\n" +
              "  * korisnik_id : number <<FK>>\n" +
              "}\n\n" +
              "entity Stavka {\n" +
              "  * id : number <<generated>>\n" +
              "  --\n" +
              "  * narudzba_id : number <<FK>>\n" +
              "  * proizvod_id : number <<FK>>\n" +
              "  * kolicina : number\n" +
              "}\n\n" +
              "Korisnik ||--o{ Narudzba\n" +
              "Narudzba ||--o{ Stavka\n" +
              "Proizvod ||--o{ Stavka\n" +
              "@enduml"
            }
        };

        public static void Initialize()
        {
            try
            {
                string directory = Path.GetDirectoryName(_templatesFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(_templatesFilePath))
                {
                    File.WriteAllText(_templatesFilePath, JsonConvert.SerializeObject(_defaultTemplates, Formatting.Indented));
                }

                LoadTemplates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška prilikom inicijalizacije predložaka: {ex.Message}",
                    "Greška", MessageBoxButton.OK, MessageBoxImage.Error);

                Templates = new Dictionary<string, string>(_defaultTemplates);
            }
        }

        public static void AddTemplate(string name, string content)
        {
            Templates[name] = content;
            SaveTemplates();
        }

        public static void RemoveTemplate(string name)
        {
            if (Templates.ContainsKey(name))
            {
                Templates.Remove(name);
                SaveTemplates();
            }
        }

        public static void SaveTemplates()
        {
            try
            {
                string directory = Path.GetDirectoryName(_templatesFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(Templates, Formatting.Indented);
                File.WriteAllText(_templatesFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška prilikom spremanja predložaka: {ex.Message}",
                    "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void LoadTemplates()
        {
            try
            {
                if (File.Exists(_templatesFilePath))
                {
                    string json = File.ReadAllText(_templatesFilePath);
                    Templates = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                    if (Templates == null)
                    {
                        Templates = new Dictionary<string, string>(_defaultTemplates);
                        SaveTemplates();
                    }
                }
                else
                {
                    Templates = new Dictionary<string, string>(_defaultTemplates);
                    SaveTemplates();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška prilikom učitavanja predložaka: {ex.Message}",
                    "Greška", MessageBoxButton.OK, MessageBoxImage.Error);

                Templates = new Dictionary<string, string>(_defaultTemplates);
            }
        }

        public static void ResetToDefaultTemplates()
        {
            try
            {
                Templates = new Dictionary<string, string>(_defaultTemplates);
                SaveTemplates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška prilikom resetiranja predložaka: {ex.Message}",
                    "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
