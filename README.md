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

Gateway servis poseduje tri kontrolera. Prilikom pogodaka endpoint-a generiše komande `CreateActivity` i `CreateUser`. Ove komande konzumiraju ostala dva servisa, menjaju lokalne baze podataka i odgovaraju događajima `ActivityCreated` i `UserCreated`. Ukoliko podaci nisu odgovarajući, odgovaraju na komande događajima `CreateActivityRejected` i `CreateUserRejected`. Pomenute događaje odgovore konzumira gateway servis.

`HomeController` nema posebne funkcionalnosti osim osnovnog šablonskog endpoint-a.

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

#### `root/api/activities`

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

Dakle, osnovna funkcionalnost gateway servisa je koordinacija komandi i događaja. Pored toga, sadrži i kopije podataka `Microservices.Services.Activities` servisa kojima se odgovara na `GET` zahteve, a suprotno tome, u slučaju `POST` zahteva se kontaktiraju servisi `Microservices.Services.Activities` i `Microservices.Services.Identity` kako bi modifikovali lokalne baze podataka.

#### `Microservices.Services.Identity`

Ovaj servis poseduje jedan kontroler zadužen za prijavljivanje korisnika. Pored endpointa za prijavljivanje, reaktivan je na komandu `CreateUser` koju emituje gateway servis - gde će posledica biti dodavanje korisnika u lokalnu bazu podataka.

`AccountController` na ruti `/login` očekuje email i password korisnika i kao rezultat vraća **JsonWebToken** koji zatim treba koristiti prilikom gađanja ostalih endpoint-a gateway servisa.

##### `root/`

```c#
namespace Microservices.Services.Identity.Controllers
{
    [Route("")]
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthenticateUser command)
            => Json(await _userService.LoginAsync(command.Email, command.Password));

        [HttpGet("")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult Get() => Content("Secured_API");
    }
}
```

#### `Microservices.Services.Activities`

Ovaj servis ne poseduje API već je samo reaktivan na komande koje emituje gateway servis. Komanda koju osluškuje je `CreateActivity` i tada dolazi do modifikacije lokalne baze podataka i dodavanja prosleđenih podataka u vidu još jedne aktivnosti. 

Odgovor na komandu se sastoji od **dodavanja `recorda` u bazu lokalnu podataka** i **emitovanje događaja `ActivityCreated`** na koji je reaktivan gateway servis.

```c#
public async Task HandleAsync(CreateActivity command)
{
    _logger.LogInformation($"Creating activity: '{command.Id}' for user: '{command.UserId}'.");
    try
    {
        await _activityService.AddAsync(command.Id, command.UserId,
            command.Category, command.Name, command.Description, command.CreatedAt);
        await _busClient.PublishAsync(new ActivityCreated(command.Id,
            command.UserId, command.Category, command.Name, command.Description, command.CreatedAt));
        _logger.LogInformation($"Activity: '{command.Id}' was created for user: '{command.UserId}'.");

        return;
    }
    catch (MicroserviceException ex)
    {
        _logger.LogError(ex, ex.Message);
        await _busClient.PublishAsync(new CreateActivityRejected(command.Id,
            ex.Message, ex.Code));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, ex.Message);
        await _busClient.PublishAsync(new CreateActivityRejected(command.Id,
            ex.Message, "error"));
    }
}
```

### Korisnički interfejs

Kako je fokus projekta na mikroservisima i komunikaciji, korisnički interfejs je šablonski je koristi se `SwaggerUI` ekstenzija. Dva servisa koja poseduju endpoint-e imaju i `/swagger` rute koje mogu da se koriste za testiranje.

Na slici se može videti interfejs gateway servisa. Sličan interfejs poseduje i identity servis.

![alt text][api_ui]

[api_ui]: meta/api_ui.png

## Lokalno testiranje servisa

Svaki od projekata treba odvojeno pokrenuti na posebnom portu.

`$ dotnet run` za `Microservices.Api`

`$ dotnet run --urls "http://localhost:5050"` za `Microservices.Services.Identity`

`$ dotnet run --urls "http://localhost:5051"` za `Microservices.Services.Activities`

Nakon pokretanja treba očekivati sledeći output konzole:

```sh
$ dotnet run >
Hosting environment: Development
Content root path: ...\src\Microservices.Api
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.

```

Kako servisi zavise od **MongoDB** i **RabbitMQ** okruženja neophodno je pokrenuti i odgovarajuće docker kontejnere.

`$ docker run -d -p 27017:27017 mongo`

`$ docker run -p 5672:5672 rabbitmq`

### Testiranje API endpointa

#### `POST http://gateway_api/api/users` za registrovanje

```sh
$ curl --location --request POST 'http://localhost:5000/api/users?=' \
--header 'Content-Type: application/json' \
--data-raw '{
	"email": "dusanjovanovic@stud.ntnu.no", 
	"password": "secret",
	"name": "Dusan"
}'
```

#### `POST http://identity_api/login` za prijavljivanje

```sh
$ curl --location --request POST 'http://localhost:5050/login' \
--header 'Content-Type: application/json' \
--data-raw '{
	"email": "dusanjovanovic@stud.ntnu.no",
	"password": "secret"
}'
```

Odgovor koji sadrži token koji zatim treba koristiti u ostalim zahtevima:

```sh
{
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI0ZmJjYzc0Zi05OTUzLTRmZjgtODA4My0wNTBlMDBhMzk1OGIiLCJpc3MiOiJodHRwOi8vbG9jYWxob3N0OjUwMDAiLCJpYXQiOjE1ODI4MTU4ODMsImV4cCI6MTU4MjgxOTQ4MywidW5pcXVlX25hbWUiOiI0ZmJjYzc0Zi05OTUzLTRmZjgtODA4My0wNTBlMDBhMzk1OGIifQ.R1i5c-dlSqT1BBYBsDXsq7sRz5NEDaXEV4xX7Vqugi4",
    "expires": 1582819483
}
```

#### `POST http://gateway_api/api/activities` za dodavanje aktivnosti

```sh
$ curl --location --request POST 'http://localhost:5000/api/activities' \
--header 'Content-Type: application/json' \
--header 'Authorization: bearer <token>' \
--data-raw '{
	"name": "some_activity",
	"category": "work",
	"description": "some activity description.."
}'
```

#### `GET http://gateway_api/api/activities` za pribavljanje svih aktivnosti

```sh
$ curl --location --request GET 'http://localhost:5000/api/activities' \
--header 'Content-Type: application/json' \
--header 'Authorization: bearer <token>'
```

Odgovor:

```sh
[
    {
        "id": "ba4feb21-be1d-4f6f-8a26-1a161ff9a26e",
        "name": "some_activity",
        "category": "work",
        "createdAt": "2020-02-27T14:06:17.106Z"
    },
    {
        "id": "73df2f44-c2c1-4b97-8c21-6bd31a0e67ce",
        "name": "another_activity",
        "category": "work",
        "createdAt": "2020-02-27T14:07:25.768Z"
    }
]
```

#### `GET http://gateway_api/api/activities/{Id}` za pribavljanje jedne aktivnosti

```sh
$ curl --location --request GET 'http://localhost:5000/api/activities/ba4feb21-be1d-4f6f-8a26-1a161ff9a26e' \
--header 'Content-Type: application/json' \
--header 'Authorization: bearer <token>'
```

Odgovor:

```sh
{
    "id": "ba4feb21-be1d-4f6f-8a26-1a161ff9a26e",
    "userId": "4fbcc74f-9953-4ff8-8083-050e00a3958b",
    "category": "work",
    "name": "some_activity",
    "description": "some activity description..",
    "createdAt": "2020-02-27T14:06:17.106Z"
}
```

#### Snapshot baza podataka

Baza podataka `Microservices-Activities`>

```
Collection[Activities]:
{"_id":{"$binary":{"base64":"BwxDEKZXKU2yYr1nwdO5HA==","subType":"03"}},"userId":{"$binary":{"base64":"DVOL0oArtkCnKedJXjw5/w==","subType":"03"}},"name":"activity_01","category":"work","description":"Work activity","createdAt":{"$date":{"$numberLong":"1582750398263"}}}
{"_id":{"$binary":{"base64":"IetPuh2+b0+KJhoWH/mibg==","subType":"03"}},"userId":{"$binary":{"base64":"T8e8T1OZ+E+AgwUOAKOViw==","subType":"03"}},"name":"some_activity","category":"work","description":"some activity description..","createdAt":{"$date":{"$numberLong":"1582812377106"}}}
{"_id":{"$binary":{"base64":"RC/fc8HCl0uMIWvTGg5nzg==","subType":"03"}},"userId":{"$binary":{"base64":"T8e8T1OZ+E+AgwUOAKOViw==","subType":"03"}},"name":"another_activity","category":"work","description":"another activity description..","createdAt":{"$date":{"$numberLong":"1582812445768"}}}
	
Collection[Categories]:
{"_id":{"$binary":{"base64":"W2li27zID0mpWh6/JueCIQ==","subType":"03"}},"name":"work"}
{"_id":{"$binary":{"base64":"UKN3+7oWlkKrJzEfsOQ33w==","subType":"03"}},"name":"hobby"}
{"_id":{"$binary":{"base64":"dCUvuGK6q0WJf35H9yN6Qg==","subType":"03"}},"name":"sport"}
```

Baza podataka `Microservices-Identity`>

```
Collection[Users]:
{"_id":{"$binary":{"base64":"DVOL0oArtkCnKedJXjw5/w==","subType":"03"}},"email":"dusanjov@elfak.rs","name":"Dusan","password":"OG2g4W/WUHIgAnU9l3NDtCCHSZkbBPQL2tXTdtz3JQSgbx1zlRAiiQ==","salt":"mHIGEH6uFGBXKqohEWloSNbiv1092CzxUb2YCPxDfi73kOSrTDQ11w==","createdAt":{"$date":{"$numberLong":"1582736041381"}}}
{"_id":{"$binary":{"base64":"T8e8T1OZ+E+AgwUOAKOViw==","subType":"03"}},"email":"dusanjovanovic@stud.ntnu.no","name":"Dusan","password":"WACqoRok1fWUrsuqI2WpHiqh6c0r6G4wJdHepKtelWs9HN35oTbX+w==","salt":"0Nt3KsY+R/4HKUDs/TL7ZgLws2XOddp/aO3y3b++E4q2z7IbDUbY7Q==","createdAt":{"$date":{"$numberLong":"1582812187457"}}}

```

Baza podataka `Microservices-Gateway`>

```
Collection[Activities]:
{"_id":{"$binary":{"base64":"BwxDEKZXKU2yYr1nwdO5HA==","subType":"03"}},"userId":{"$binary":{"base64":"DVOL0oArtkCnKedJXjw5/w==","subType":"03"}},"category":"work","name":"activity_01","description":"Work activity","createdAt":{"$date":{"$numberLong":"1582750398263"}}}
{"_id":{"$binary":{"base64":"IetPuh2+b0+KJhoWH/mibg==","subType":"03"}},"userId":{"$binary":{"base64":"T8e8T1OZ+E+AgwUOAKOViw==","subType":"03"}},"category":"work","name":"some_activity","description":"some activity description..","createdAt":{"$date":{"$numberLong":"1582812377106"}}}
{"_id":{"$binary":{"base64":"RC/fc8HCl0uMIWvTGg5nzg==","subType":"03"}},"userId":{"$binary":{"base64":"T8e8T1OZ+E+AgwUOAKOViw==","subType":"03"}},"category":"work","name":"another_activity","description":"another activity description..","createdAt":{"$date":{"$numberLong":"1582812445768"}}}


```

## Docker kontejneri

Iz svega do sada priloženog može se primetiti da je sistem podeljen na više zasebnih komponenti. Postoje tri zasebna mikroservisa, kao i instance **MongoDB baze podataka** i **RabbitMQ brokera**.

Prema tome, biće potrebno ukupno pet docker kontejnera.

### DotNet kontejneri

Za svaki od servisa je neophodan **image i kontejner** u kome će se izvršavati. Prema tome, neophodno je formirati instrukcije kako **build-ovati image** na osnovu datoteke `Dockerfile`.

Na primeru `Microservices.Api` mikroservisa:

```dockerfile
FROM microsoft/dotnet:2.0.0-runtime
WORKDIR /dotnetapp
COPY ./bin/Docker .
ENV ASPNETCORE_URLS http://*:5000
ENV ASPNETCORE_ENVIRONMENT docker
ENTRYPOINT dotnet Microservices.Api.dll
```

Zatim, neophodno je uraditi:
* `$ dotnet publish -c Release -o ./bin/Docker` za objavljivanje projekta
* `$ docker build -t microservices.api .` za build-ovanje image instance
* `$ docker run -p 5000:5000 microservices.api` za lokalno pokretanje
* `$ docker tag microservices.api dusandjovanovic/microservices.api $ docker push dusandjovanovic/microservices.api` za postavljanje image datoteke na Hub

Ovo je postupak za svaki od servisa. Takođe **MongoDB** i **RabbitMQ** okruženja se izvršavaju u **posebnim kontejnerima**.
