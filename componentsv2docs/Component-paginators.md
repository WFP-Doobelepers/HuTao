## Overview

Component paginators are a new paginator type written from scratch with customization and flexibility in mind.
They exclusively use interactions and components for navigation and have native support for components V2.

One of the most significant changes from traditional paginators is that components are now fully decoupled from the paginator.
The page factory now has complete control over which components are displayed in the message while still allowing the paginator to loosely own or manage the navigation buttons.

Component paginators were developed in response to the increasing demand for more sophisticated pagination systems, including nested or 2D pages and the ability to switch between sets of pages using select menus. It was impossible to create these systems with regular paginators without custom classes or hacky workarounds due to fundamental design issues in the original paginator.

Another reason for creating component paginators is that regular paginators aren't compatible with components V2 at all.

Fergun.Interactive provides a default implementation of the component paginator (`ComponentPaginator`, which is created using `ComponentPaginatorBuilder`).

## Usage

### Ignoring interactions meant for paginators

In your interaction handlers, make sure to ignore interactions meant for paginators by adding `InteractiveService.IsManaged`:
```c#
private async Task HandleInteractionAsync(SocketInteraction interaction)
{
    if (_interactive.IsManaged(interaction))
        return;

    var context = new SocketInteractionContext(_client, interaction);
    await _commands.ExecuteCommandAsync(context, _services);
}

```

### Simple paginator

Here's an example of a component paginator that displays a simple embed, explaining (in comments) the differences from regular paginators:
```c#
using Fergun.Interactive;
using Fergun.Interactive.Pagination;

[SlashCommand("send", "Sends a simple paginator.")]
public async Task SendPaginatorAsync()
{
    // The builder methods are now extension methods, residing on the Fergun.Interactive.Pagination namespace
    var paginator = new ComponentPaginatorBuilder()
        .AddUser(Context.User) 
        .WithPageFactory(GeneratePage)
        .WithPageCount(10) // Now takes a page count instead of max. page index
        .Build();

    await _interactive.SendPaginatorAsync(paginator, Context.Interaction, TimeSpan.FromMinutes(10));

    // The factory method now takes the built paginator as an argument
    // You can use the CurrentPageIndex property to retrieve the page index
    IPage GeneratePage(IComponentPaginator p) 
    {
        var components = new ComponentBuilder()
            .AddPaginatorButtons(p) // Extension method that adds the standard navigation buttons, mimicking the behavior from regular paginators
            .Build();

        return new PageBuilder()
            .WithDescription($"This is page {p.CurrentPageIndex + 1}.")
            .WithColor(Color.Blue)
            .WithPaginatorFooter(p) // Adds the standard paginator footer through an extension method
            .WithComponents(components)
            .Build();
    }
}
```

Component paginators require more manual handling, but that makes them much more powerful than regular paginators.

They always use a page factory too, by design.

### Handling component interactions and storing state

Component paginators now allow you to store arbitrary state. This is useful for storing data that needs to be retrieved elsewhere, such as component interaction commands.

Along with the ability to render a page through `IComponentPaginator.RenderAsync`, this allows you to modify messages to contain pages that aren't triggered by the standard navigation buttons. It also allows for complex pagination systems in a clean way.

The following example command demonstrates these new features:
```c#
using Fergun.Interactive;
using Fergun.Interactive.Pagination;

[SlashCommand("wikipedia", "Sends a paginator that allows switching over a set of pages.")]
public async Task WikipediaAsync()
{
    string[] description =
    [
        "In computing, just-in-time (JIT) compilation (also dynamic translation or run-time compilations) is compilation (of computer code) during execution of a program (at run time) rather than before execution.",
        "This may consist of source code translation but is more commonly bytecode translation to machine code, which is then executed directly."
    ];

    string[] history =
    [
        "The earliest published JIT compiler is generally attributed to work on LISP by John McCarthy in 1960.",
        "Smalltalk (c. 1983) pioneered new aspects of JIT compilations. For example, translation to machine code was done on demand, and the result was cached for later use.",
        "Sun's Self language improved these techniques extensively and was at one point the fastest Smalltalk system in the world, achieving up to half the speed of optimized C but with a fully object-oriented language.",
        """
        Self was abandoned by Sun, but the research went into the Java language. The term "Just-in-time compilation" was borrowed from the manufacturing term "Just in time" and popularized by Java, with James Gosling using the term from 1993.
        Currently JITing is used by most implementations of the Java Virtual Machine, as HotSpot builds on, and extensively uses, this research base.
        """
    ];

    string[] design =
    [
        """
        In a bytecode-compiled system, source code is translated to an intermediate representation known as bytecode. Bytecode is not the machine code for any particular computer, and may be portable among computer architectures.
        The bytecode may then be interpreted by, or run on a virtual machine. The JIT compiler reads the bytecodes in many sections (or in full, rarely) and compiles them dynamically into machine code so the program can run faster.
        """,
        """
        By contrast, a traditional interpreted virtual machine will simply interpret the bytecode, generally with much lower performance. Some interpreters even interpret source code, without the step of first compiling to bytecode, with even worse performance.
        Statically-compiled code or native code is compiled prior to deployment. A dynamic compilation environment is one in which the compiler can be used during execution.
        """
    ];

    var sections = new Dictionary<string, string[]>
    {
        ["Description"] = description,
        ["History"] = history,
        ["Design"] = design
    };

    var state = new WikipediaState(sections, sections.Keys.First());

    var paginator = new ComponentPaginatorBuilder()
        .WithUsers(Context.User)
        .WithPageCount(state.Sections[state.CurrentSectionName].Length)
        .WithUserState(state) // Attach the state into the paginator so we can retrieve it elsewhere
        .WithPageFactory(GeneratePage)
        .WithActionOnCancellation(ActionOnStop.DeleteMessage)
        .WithActionOnTimeout(ActionOnStop.DisableInput)
        .Build();

    await _interactive.SendPaginatorAsync(paginator, Context.Interaction, TimeSpan.FromMinutes(10));

    IPage GeneratePage(IComponentPaginator p)
    {
        // Create the select menu options from the dictionary keys
        var options = state.Sections.Keys
            .Select(x => new SelectMenuOptionBuilder(x, x, isDefault: x == state.CurrentSectionName))
            .ToList();

        var section = state.Sections[state.CurrentSectionName];
    
        var components = new ComponentBuilderV2()
            .WithContainer(new ContainerBuilder()
                .WithTextDisplay($"## Just-in-time compilation\n{section[p.CurrentPageIndex]}")
                .WithActionRow(new ActionRowBuilder() // Interactions targeting this select menu will be handled on SelectSectionAsync
                    .WithSelectMenu("paginator-select-section", options, disabled: p.ShouldDisable()))
                .WithActionRow(new ActionRowBuilder()
                    .AddPreviousButton(p, style: ButtonStyle.Secondary) // Add navigation buttons managed by the paginator
                    .AddNextButton(p, style: ButtonStyle.Secondary)
                    .AddStopButton(p))
                .WithSeparator()
                .WithTextDisplay($"Info by Wikipedia | Page {p.CurrentPageIndex + 1} of {p.PageCount}")
                .WithAccentColor(Color.Blue))
            .Build();

        return new PageBuilder()
            .WithComponents(components) // Using components V2 requires not setting the page text, stickers, or any embed property
            .Build();
    }
}

// This method handles the select menu from the paginator and stores the new section name in the attached state
// It also sets the page count, current page index, and renders the page
[ComponentInteraction("paginator-select-section", ignoreGroupNames: true)]
public async Task SelectSectionAsync(string sectionName)
{
    var interaction = (IComponentInteraction)Context.Interaction;

    // RenderPageAsync bypasses the user check, so we need to call CanInteract here.
    if (!_interactive.TryGetComponentPaginator(interaction.Message, out var paginator) || !paginator.CanInteract(interaction.User))
    {
        await DeferAsync();
        return;
    }

    var state = paginator.GetUserState<WikipediaState>(); // Extension method that gets the user state from the paginator as WikipediaState

    state.CurrentSectionName = sectionName;
    paginator.SetPage(0); // Reset the page index to 0
    paginator.PageCount = state.Sections[sectionName].Length; // Set the new page count

    await paginator.RenderPageAsync(interaction); // Render the current page of the paginator, this will call the GeneratePage method
}

public class WikipediaState
{
    public WikipediaState(Dictionary<string, string[]> sections, string currentSectionName)
    {
        Sections = sections;
        CurrentSectionName = currentSectionName;
    }

    public Dictionary<string, string[]> Sections { get; }

    public string CurrentSectionName { get; set; }
}
```

### Multi-step paginator

Component paginators also allow you to have multi-step selection with pagination and buttons to change the step:
```c#
using Fergun.Interactive;
using Fergun.Interactive.Pagination;

[SlashCommand("travel-selection", "Sends a multi-step paginator and the ability to select items and to go to a previous step.")]
public async Task TravelSelectionAsync()
{
    // The name of these places may be wrong, but they are just used for demonstration purposes
    var africa = new Dictionary<string, string[]>
    {
        ["Egypt"] = ["Cairo", "Alexandria", "Giza", "Luxor", "Aswan", "Sharm El Sheikh"],
        ["South Africa"] = ["Cape Town", "Johannesburg", "Durban"],
        ["Nigeria"] = ["Lagos", "Abuja", "Kano", "Ibadan", "Port Harcourt", "Calabar"],
        ["Kenya"] = ["Nairobi", "Mombasa", "Kisumu", "Nakuru", "Eldoret", "Malindi"],
        ["Morocco"] = ["Marrakech", "Casablanca", "Fes", "Rabat", "Tangier", "Agadir"],
        ["Tanzania"] = ["Dar es Salaam", "Arusha", "Zanzibar City", "Dodoma", "Moshi"]
    };

    var asia = new Dictionary<string, string[]>
    {
        ["China"] = ["Beijing", "Shanghai", "Xi'an", "Guangzhou", "Chengdu", "Shenzhen"],
        ["India"] = ["Delhi", "Mumbai", "Agra", "Jaipur", "Bangalore", "Kolkata"],
        ["Japan"] = ["Tokyo", "Kyoto", "Osaka", "Hiroshima", "Nara", "Sapporo"],
        ["South Korea"] = ["Seoul", "Busan", "Incheon", "Jeju", "Gyeongju", "Daegu"],
        ["Indonesia"] = ["Jakarta", "Bali", "Yogyakarta", "Surabaya", "Bandung", "Lombok"],
        ["Thailand"] = ["Bangkok", "Chiang Mai", "Phuket", "Pattaya", "Ayutthaya", "Krabi"]
    };

    var europe = new Dictionary<string, string[]>
    {
        ["Germany"] = ["Berlin", "Munich", "Frankfurt", "Hamburg", "Cologne", "Dresden"],
        ["France"] = ["Paris", "Lyon", "Marseille", "Bordeaux", "Strasbourg"],
        ["Italy"] = ["Rome", "Venice", "Florence", "Milan", "Naples", "Pisa"],
        ["Spain"] = ["Madrid", "Barcelona", "Seville", "Valencia", "Granada", "Bilbao"],
        ["United Kingdom"] = ["London", "Edinburgh", "Manchester", "Liverpool", "Oxford"],
        ["Netherlands"] = ["Amsterdam", "Rotterdam", "Utrecht", "The Hague"]
    };

    var northAmerica = new Dictionary<string, string[]>
    {
        ["United States"] = ["New York", "Los Angeles", "Las Vegas", "San Francisco", "Chicago", "Miami"],
        ["Canada"] = ["Toronto", "Vancouver", "Montreal", "Quebec City", "Calgary", "Ottawa"],
        ["Mexico"] = ["Mexico City", "Cancun", "Guadalajara", "Monterrey", "Tulum", "Playa del Carmen"],
        ["Cuba"] = ["Havana", "Varadero", "Santiago de Cuba", "Trinidad", "Cienfuegos", "Camagüey"],
        ["Dominican Republic"] = ["Punta Cana", "Santo Domingo", "Puerto Plata", "La Romana", "Samaná", "Bávaro"],
        ["Guatemala"] = ["Guatemala City", "Antigua", "Flores", "Panajachel", "Quetzaltenango", "Livingston"]
    };

    var southAmerica = new Dictionary<string, string[]>
    {
        ["Brazil"] = ["Rio de Janeiro", "São Paulo", "Salvador", "Brasilia", "Fortaleza", "Manaus"],
        ["Argentina"] = ["Buenos Aires", "Mendoza", "Bariloche", "Córdoba", "Ushuaia", "Salta"],
        ["Colombia"] = ["Bogotá", "Medellín", "Cartagena", "Cali", "Santa Marta", "Barranquilla"],
        ["Chile"] = ["Santiago", "Valparaíso", "Viña del Mar", "Puerto Varas", "Pucón"],
        ["Peru"] = ["Lima", "Cusco", "Arequipa", "Trujillo", "Puno"],
        ["Venezuela"] = ["Caracas", "Mérida", "Maracaibo", "Valencia", "Puerto La Cruz", "Canaima"]
    };

    var oceania = new Dictionary<string, string[]>
    {
        ["New Zealand"] = ["Auckland", "Wellington", "Queenstown", "Christchurch", "Rotorua", "Dunedin"],
        ["Australia"] = ["Sydney", "Melbourne", "Brisbane", "Perth", "Adelaide", "Cairns"],
        ["Papua New Guinea"] = ["Port Moresby", "Lae", "Madang", "Mount Hagen", "Kokopo", "Wewak"],
        ["Fiji"] = ["Suva", "Nadi", "Lautoka", "Savusavu", "Labasa", "Coral Coast"],
        ["Tonga"] = ["Nuku'alofa", "Vava'u", "Ha'apai"],
        ["Samoa"] = ["Apia", "Savai'i", "Upolu", "Lalomanu", "Manono"]
    };

    var continents = new Dictionary<string, Dictionary<string, string[]>>
    {
        ["Africa"] = africa,
        ["Asia"] = asia,
        ["Europe"] = europe,
        ["North America"] = northAmerica,
        ["South America"] = southAmerica,
        ["Oceania"] = oceania
    };

    // The state will hold the continents and will help us to choose which items to display and how many pages the paginator should have
    var state  = new TravelSelectionState(continents);

    var paginator = new ComponentPaginatorBuilder()
        .WithUsers(Context.User)
        .WithPageCount(state.TotalPages)
        .WithUserState(state)
        .WithPageFactory(GeneratePage)
        .WithActionOnCancellation(ActionOnStop.DeleteMessage)
        .WithActionOnTimeout(ActionOnStop.DisableInput)
        .Build();

    await _interactive.SendPaginatorAsync(paginator, Context.Interaction, TimeSpan.FromMinutes(10));

    IPage GeneratePage(IComponentPaginator p)
    {
        var builder = new ComponentBuilder()
            .WithButton("Back", "travel-selection-back", ButtonStyle.Secondary, disabled: state.Step == TravelSelectionStep.Continent)
            .AddPreviousButton(p, style: ButtonStyle.Secondary)
            .AddNextButton(p, style: ButtonStyle.Secondary)
            .AddStopButton(p);

        string[] items = [];
        if (state.Step != TravelSelectionStep.End)
        {
            items = state.CurrentItems
                .Chunk(TravelSelectionState.ItemsPerPage) // Split the items into chunks to allow pagination. For example, if there are 6 items, they will be split into 2 chunks of 3 items each
                .ElementAt(p.CurrentPageIndex);

            var options = items
                .Select(x => new SelectMenuOptionBuilder(x, x))
                .ToList();

            builder.WithSelectMenu("travel-selection-next", options, disabled: p.ShouldDisable());
        }

        string description = state.Step == TravelSelectionStep.End
            ? $"- Selected continent: {state.SelectedContinent}\n- Selected country: {state.SelectedCountry}\n- Selected place: {state.SelectedPlace}"
            : $"Select a {state.Step.ToString().ToLowerInvariant()}:\n{string.Concat(items.Select(x => $"- {x}\n"))}";

        string footer = state.Step == TravelSelectionStep.End
            ? "Note: You can go back to select a different place, country or continent."
            : $"Page {p.CurrentPageIndex + 1} of {p.PageCount}";

        return new PageBuilder()
            .WithTitle("Travel Destination Planner")
            .WithDescription(description)
            .WithFooter(footer)
            .WithColor(Color.Blue)
            .WithComponents(builder.Build())
            .Build();
    }
}

[ComponentInteraction("travel-selection-back", ignoreGroupNames: true)]
public async Task TravelSelectionBackAsync()
    => await TravelSelectionChangeStepAsync();

[ComponentInteraction("travel-selection-next", ignoreGroupNames: true)]
public async Task TravelSelectionNextAsync(string selected)
    => await TravelSelectionChangeStepAsync(selected);

// Since both the select menu and back button serve similar functions, the logic can be combined into one method
private async Task TravelSelectionChangeStepAsync(string? selected = null)
{
    var interaction = (IComponentInteraction)Context.Interaction;
    if (!_interactive.TryGetComponentPaginator(interaction.Message, out var paginator) || !paginator.CanInteract(interaction.User))
    {
        await DeferAsync();
        return;
    }

    var state = paginator.GetUserState<TravelSelectionState>();

    if (selected is null)
    {
        state.Step--;
    }
    else
    {
        if (state.Step == TravelSelectionStep.Continent)
            state.SelectedContinent = selected;
        else if (state.Step == TravelSelectionStep.Country)
            state.SelectedCountry = selected;
        else if (state.Step == TravelSelectionStep.Place)
            state.SelectedPlace = selected;

        state.Step++;
    }

    paginator.PageCount = state.TotalPages;
    paginator.SetPage(0);

    await paginator.RenderPageAsync(interaction);
}

public class TravelSelectionState
{
    public const int ItemsPerPage = 3;

    public TravelSelectionState(Dictionary<string, Dictionary<string, string[]>> continents)
    {
        Continents = continents;
    }

    public IReadOnlyCollection<string> CurrentItems => Step switch
    {
        TravelSelectionStep.Continent => Continents.Keys,
        TravelSelectionStep.Country => Continents[SelectedContinent!].Keys,
        TravelSelectionStep.Place => Continents[SelectedContinent!][SelectedCountry!],
        _ => []
    };

    public int TotalPages => Step == TravelSelectionStep.End ? 1 : (int)Math.Ceiling((double)CurrentItems.Count / ItemsPerPage);

    public Dictionary<string, Dictionary<string, string[]>> Continents { get; }
    
    public TravelSelectionStep Step { get; set; } = TravelSelectionStep.Continent;
    
    public string? SelectedContinent { get; set; }
    
    public string? SelectedCountry { get; set; }
    
    public string? SelectedPlace { get; set; }
}


public enum TravelSelectionStep
{
    Continent,
    Country,
    Place,
    End
}
```

## Other changes from the original paginator
- Component paginators now bypass the single-page check on `InteractiveConfig`, meaning they will always be processed
- Component paginators now allow full control of the jump modal that will be displayed (via `JumpModalFactory`)
- Valid integer inputs from jump modal interactions will be auto-clamped to the range of valid pages, and interactions with invalid inputs will be deferred
- Now it's possible to force a page render by setting `ActionOnStop.ModifyMessage` on `ActionOnCancellation` or `ActionOnTimeout`, provided there are no canceled or timed-out pages
- Due to the aforementioned feature, now `ComponentPaginatorBuilder.ActionOnCancellation` and `ComponentPaginatorBuilder.ActionOnTimeout` will default to `ActionOnStop.None`. The extension methods that set the cancel and timeout page will automatically set `ActionOnStop.ModifyMessage` to those properties
- All the page rendering and management methods now reside inside `ComponentPaginator`
- `RestrictedPageFactory` now takes the current paginator as argument instead of a read-only collection of users