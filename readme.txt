cont admin:
admin@cinema.com
Vb@2s6XQahut@4E





BAREM:

Nota 2 (Done)

1) Baza de date relationala proiectata corect (3NF) folosind Entity Framework Core.
2) Minimum 4 scenarii functionale: vizualizare filme, rezervare bilete (Cash/Stripe),
gestiune profil (avatar) si administrare filme.
3) Arhitectura software MVC (Model-View-Controller) impreuna cu pattern-ul
de servicii (IMovieService).
4) Elemente multimedia prezente: imagini (postere/avatare), tabele de date
si linkuri de navigare.


Nota 3 (Done)

1) Utilizarea claselor pentru entitati (Movie, Screening, Booking),
servicii (MovieService) si controllere.
2) Utilizarea interfețelor (IMovieService) pentru separarea logicii
de business (Dependency Injection).
3) Autentificarea utilizatorilor implementata complet prin ASP.NET Core Identity.
4) Autorizarea pe bază de roluri (Admin / User) prin atribute de securitate [Authorize].


Nota 4 (3/4 Done. 4) trebuie verificat) 

1) Mostenire: Implementata printr-o ierarhie de clase personalizate pentru exceptii,
unde clasa SeatUnavailableException mosteneste clasa parinte CinemaException.
2) Gestionarea exceptiilor: Sunt tratate multiple tipuri de exceptii in blocuri
try-catch dedicate (exceptii custom de business: SeatUnavailableException,
CinemaException, alaturi de exceptia generala de sistem Exception).
3) Carusel de imagini: Implementat si functional pe pagina principala (Index)
pentru prezentarea dinamica a posterelor si filmelor.
4) Animatii CSS: Efecte vizuale interactive de tranzitie si hover aplicate
pe butoane, postere si elementele de selectie a scaunelor.


Nota 5 ( 1/2 Done. trebuie verificat telefon la 2) )

1) Organizarea stilurilor CSS: Proiectul contine multiple fisiere CSS dedicate
 (site.css, stiluri pentru pagina principala si bootstrap.min.css)
 si utilizeaza peste 25 de proprietati CSS distincte (radial-gradient
 backdrop-filter, transform, transition, box-shadow, filter blur, variabile CSS).
2) Design responsive: Interfata se adapteaza automat pe desktop,
tableta si mobil prin sistemul Bootstrap Grid (container, row, col-lg),
unitati relative (%, rem) si optimizari tactile (-webkit-overflow-scrolling: touch)
pentru carusel si slider-ul de filme.







Nota 7 (Done)

1) Logging:
- La rezervari BookingsController;
- La Profilul Utilizatorului ProfileController;

2) Gazduirea aplicatiei / hosting
- Detalii: Site-ul este publicat si accesibil online prin intermediu
 serviciului GitHub Pages (index.html), respectand cerinta de publicare online din barem.

Nota 8 (Done)

1) Optimizare SEO (0.50 pct)
1) Aplicatia este optimizata SEO conform standardelor (meta viewport,
meta description configurata corect, atribute semantice HTML5)
si obtine scorul maxim de 100/100 la categoria
SEO in Google Lighthouse atat pe desktop, cat si pe mobile.
2) Sistem de plati online (0.50 pct)
- Detalii: Este integrat un sistem functional de plati online securizate
folosind Stripe API, cu gestionarea sigura a cheilor API prin .NET User Secrets.




Nota 9 (Done)

1) Am folosit [ValidateAntiForgeryToken] pe toate acțiunile de tip 
[HttpPost] (în Controllere) și am lăsat formularele HTML să genereze 
tokenul de securitate.

2) M-am bazat pe codarea automată nativă din motorul de randare Razor al ASP.NET Core.
Oriunde afișăm date introduse de user (ex: @User.Identity?.Name),
sintaxa @ din Razor face HTML encoding automat, transformând scripturile periculoase
 in text inofensiv. Nu am folosit nicăieri @Html.Raw() pe input-uri nesecurizate.

3) Nu am scris niciodată interogări SQL brute (RAW SQL) concatenate cu string-uri.
Toate interogările folosesc Entity Framework Core și LINQ.
În controllere (ex: .FirstOrDefaultAsync(...), .Where(...)). EF Core folosește
automat interogări parametrizate (Parameterized Queries),
blocând complet orice tentativă de SQL Injection.

4) Done. Helpers/FileValidationHelper.cs MaxFileSizeBytes = 12 * 1024 * 1024

5) Done. Helpers/FileValidationHelper.cs





Nota 10

1) Este implementat un mecanism global de rate limiting care limiteaza
la maximum 100 de cereri consecutive per IP intr-un interval de un minut.
La depasirea limitei, cererile suplimentare
sunt blocate temporar cu codul HTTP 429 Too Many Requests.
2) Este implementat un sistem CAPTCHA in pagina de inregistrare pentru validarea
interactiunilor umane si limitarea interactiunilor automate, blocand scripturile
si botii sa creeze conturi in mod automat.
3) Sunt utilizate mecanisme de filtrare a fisierelor incarcate prin limitarea dimensiunii
(maximum 12 MB) si restrictionarea extensiilor permise (.pdf, .jpg, .jpeg, .png), masuri
care reduc riscul de injectare a continutului ascuns sau datelor steganografice
in cadrul platformei. Helpers/FileValidationHelper.cs
