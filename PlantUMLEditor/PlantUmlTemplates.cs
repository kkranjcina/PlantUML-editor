using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantUMLEditor
{
    internal class PlantUmlTemplates
    {
        public static Dictionary<string, string> Templates = new Dictionary<string, string>
        {
            { "Dijagram sekvenci",
              "@startuml\n" +
              "title Dijagram sekvenci\n\n" +
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
              "title Dijagram slucajeva koristenja\n\n" +
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

            { "Klasni dijagram",
              "@startuml\n" +
              "title Klasni dijagram\n\n" +
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
    }
}
