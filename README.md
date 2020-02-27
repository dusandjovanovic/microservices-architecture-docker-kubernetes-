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

Mikroservisi se *deploy-uju* pojedinačno sa **odvojenim bazama podataka po svakom servisu**. Za razliku od monolitne arhitekture, koja se bazira na jedinstvenom izvoru podataka i svim komponentama enkapsuliranim u jednom paketu, mikroservisi se baziraju na više odvojenih delova sistema koji funkcionišu ponaosob ali **mogu da razmenjuju poruke**.

![alt text][microservices_database]

[microservices_database]: meta/microservices_database.png

### Docker kontejneri

Ovaj projekat će koristiti `Docker` kontejnere koji dele resurse operativnog sistema i omogućavaju virtualizaciju.

### ASP.NET Core

`ASP.NET Core` će biti korišćena tehnologija za pisanje mikroservisa. Svaki od servisa će imati odvojeni odvojenu bazu podataka kako bi se servis mogao *deploy-ovati* odvojeno.

Pokazna aplikacija može da se koristi za čuvanje informacija o aktivnostima korisnika, a servisi od kojih se sastoji su:
* `API gateway`
* `Identity service` - autentifikacija, privilegije i informacije o registrovanim korisnicima
* `Activities service` - čuvanje podataka o aktivnostima (opis, kategorije i slično)

Servisi su pretplaćeni na `Service bus` magistralu i reaktivni da događaje koje generišu ostali servisi. Osnovna prednost ovakvog distribuiranog sistema je horizontalna skalabilnost.

## Arhitektura sistema

```
/Microservices/src
  Microservices.Common/
    Auth/
    Commands/
    Events/
    Exceptions/
    Mongo/
    RabbitMq/
    Services/
    ...
  Microservices.Api/
    Controllers/
    Handlers/
    Models/
    Repositories/
    Program.cs
    Startup.cs
    ...
  Microservices.Services.Activities/
    Controllers/
    Domain/
    Handlers/
    Repositories/
    Services/
    Program.cs
    Startup.cs
    ...
  Microservices.Services.Identity/
    Controllers/
    Domain/
    Handlers/
    Repositories/
    Services/
    Program.cs
    Startup.cs
    ...
```

`Microservices.Common` je projekat deljenih funkcionalnosti-klasa koje koriste ostali servisi. Primeri su ostvarivanje parametrizovane konekcije sa **MongoDB bazama podataka**, povezivanje na **RabbitMQ** sistem za razmenu poruka ili pak oblikovanje svih **događaja i komandi** koje se koriste u sistemu.

Nad **RabbitMQ** messaging sistemom se bazira komunikacija između servisa. Sastoji se u objavljivanju **komandi** od strane jednog servisa i konzumiranja istih od strane drugog pretplaćenog servisa u vidu izvršavanja **komande** i emitovanja **događaja**.

Direktorijum `Microservices.Common/Commands` opisuje komande:
* `ICommand`, `IAuthenticatedCmmand` interfejsi
* `CreateActivity`, `AuthenticateUser` i `CreateUser` su primeri komandi

Direktorijum `Microservices.Common/Events` opisuje događaje:
* `IEvent`, `IRejectedEvent` i `IAuthenticatedEvent` interfejsi
* `ActivityCreated`, `UserAuthenticated` i `UserCreted` su primeri događaja koji nastaju

`Microservices.Api` je gateway servis sistema i sastoji se od tri kontrolera koji oblikuju endpoint tačke. `Microservices.Services.Activities` je servis koji nema javni API već samo konzumira komande, sa druge strane, `Microservices.Services.Identity` je servis koji poput gateway servisa takođe poseduje API.

### Implementacioni detalji servisa

#### `Microservices.Api`

Gateway servis poseduje tri kontrolera.

##### `root/`

```c#
namespace Microservices.Api.Controllers
{
    [Produces("application/json")]
    [Route("")]
    public class HomeController : Controller
    {
        [HttpGet("")]
        public IActionResult Get() => Content("Hello from API!");
    }
}
```

#### `root/api/users`

`UsersController` poseduje endpoint za registrovanje novih korisnika koji će generisati odgovarajuću komandu za dodavanje korisnika. Na ovu komandu je **reaktivan servis `Microservices.Services.Identity`** koji će sačuvati potrebne informacije u bazi podataka i odgovoriti događajem o uspešnosti akcije.

```c#
namespace Microservices.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/users")]
    public class UsersController : Controller
    {
        private readonly IBusClient _busClient;

        public UsersController(IBusClient busClient)
        {
            _busClient = busClient;
        }

        [HttpPost("")]
        public async Task<IActionResult> Post([FromBody]CreateUser command)
        {
            await _busClient.PublishAsync(command);

            return Accepted();
        }
    }
}
```

### `root/api/activities`

`ActivitiesController` poseduje više endpoint-a za pribavljanje svih dodatih aktivnosti, pribavljanje konkretne aktivnosti i dodavanje novih aktivnosti. "Flattened" kopije aktivnosti se nalaze u isto vreme u bazi podataka `Microservices.Api` servisa. Takođe, prilikom dodavanja novih aktivnosti su dodate u bazi servisa `Microservices.Services.Activities`.

Prilikom `GET` zahteva se tako zbog jednostavnosti koriste kopije podataka gateway servisa, ali prilikom dodavanja novih aktivnosti treba generisati komandu koju će konzumirati servis `Microservices.Services.Activities` i reagovati dodavanjem podataka u lokalnu bazu. Nakon uspešnog dodavanja se emituje događaj na koji je reaktivan gateway servis i iste "flattened" podatke smešta u svoju bazu podataka u vidu odgovora na događaj.

```c#
[Produces("application/json")]
    [Route("api/activities")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ActivitiesController : Controller
    {
        private readonly IBusClient _busClient;
        private readonly IActivityRepository _repository;

        public ActivitiesController(IBusClient busClient,
            IActivityRepository repository)
        {
            _busClient = busClient;
            _repository = repository;
        }

        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
            var activities = await _repository
                .BrowseAsync(Guid.Parse(User.Identity.Name));

            return Json(activities.Select(x => new { x.Id, x.Name, x.Category, x.CreatedAt }));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var activity = await _repository.GetAsync(id);
            if (activity == null)
            {
                return NotFound();
            }
            if (activity.UserId != Guid.Parse(User.Identity.Name))
            {
                return Unauthorized();
            }

            return Json(activity);
        }

        [HttpPost("")]
        public async Task<IActionResult> Post([FromBody]CreateActivity command)
        {
            command.Id = Guid.NewGuid();
            command.UserId = Guid.Parse(User.Identity.Name);
            command.CreatedAt = DateTime.UtcNow;
            await _busClient.PublishAsync(command);

            return Accepted($"activities/{command.Id}");
        }
    }
```
