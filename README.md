# Arhitektura mikroservisa - kontejneri, orkestracija servisa

1. Razviti jednostavnu demo aplikaciju Web aplikaciju korišćenjem arhitekture bazirane na mikroservisima. Aplikacija treba da obezbedi dva REST API servisa pri čemu svaki servis koristi zasebnu instancu baze podataka (bazirani na projektnom obrascu **Database per service**). Servisi treba da obezbede korisnicima mogućnost da korišćenjem da HTTP GET poziva očitaju/pretraže podatke iz odgovarajuće baze podataka. Korisnički interfejs za testiranje servisa treba da bude minimalan i dovoljan da samo da samo potvrdi da servisi funkcionišu. Bar jedan servis treba da omogući i dodavanje novih podataka u pripadajuću bazu podataka (korišćenjem HTTP POST poziva). Potrebno je obezbediti i sinhronizaciju podataka između dva servisa odnosno njihovih baza podataka (podaci koje dodaje jedan servis treba sinhronizovati sa bazom podataka drugog servisa). Za sinhronizaciju iskoristiti neko message broker rešenje (Apache Kafka, RabbitMQ, ...).

2. Korišćenjem **Docker-a** kreirati kontejnere za svaku komponentu implementirane demo Web aplikacije. Potrebno je kreirati poseban kontejner za svaki REST API servis i za svaku instancu baze podataka. Ukoliko se koristi message broker u aplikaciji, potrebno je i za njega obezbediti zaseban kontejner.

3. Korišćenjem **Kubernetes-a** demonstirati mogućnosti upravljanja i orkestracije kontejnerskom aplikacijom koja je kreirana u prethodnim tačkama.

## Mikroservisi

Izraz mikroservisi se odnosi na tehnike i prakse softverskog inženjerstva koje se koriste za razvijanje skalabilnih softverskih rešenja. Pre svega se odnosi na primenu principa i arhitekturnih obrazaca. Svaki mikroservis živi pojedinačno, ali sa druge strane, mikroservisi zavise jedni od drugih. Više servisa koji čine sistem se *deploy-uju* odvojenim tempom.

![alt text][microservices_architecture]

[microservices_architecture]: meta/microservices_architecture.png

Neke od komponenti ovakve arhitekture, pored mikroservisa, su...
* **Management** - upravljanje čvorovima servisa.
* **Identity provider** - upravljanje informacijama identifikacije u distribuiranom sistemu.
* **Service discovery** - servisi, adrese i ulazne tačke servisa.
* **API gateway** - koristi se kao jedina ulazna tačka za klijentsku stranu. Ova tačka vraća odgovore sa jednog ili više (agregirano) mikroservisa.
* **CDN** - mreža za statičke deljene resurse u sistemu.

Mikroservisi se *deploy-uju* pojedinačno sa **odvojenim bazama podataka po svakom servisu**.

![alt text][microservices_database]

[microservices_database]: meta/microservices_database.png
