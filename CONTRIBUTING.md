# Contribution Guide

## Build

### Requirements
* [Git](https://git-scm.com) for cloning the project
* [.NET 5.0](https://dotnet.microsoft.com/download/dotnet/5.0) SDK
* [PostgreSQL 13](https://www.postgresql.org/download/windows) Database

### Tools
* [Visual Studio](https://visualstudio.microsoft.com) IDE
* [PostgreSQL](https://marketplace.visualstudio.com/items?itemName=RojanskyS.NpgsqlPostgreSQLIntegration) a plugin for VS to developing with PostgreSQL
* [Rider](https://www.jetbrains.com/rider) an alternative IDE that is x-platform (30 days trial)
* [Datagrip](https://www.jetbrains.com/datagrip) for managing the database (30 days trial)

### Setup

#### Repository Setup
1. Clone the repository `git clone https://github.com/WFP-Doobelepers/HuTao.git`
2. Restore the projects with `dotnet restore`.

#### User Secrets
The location will be found according to the documentation for [user secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets) in here.
  * The `<user_secrets_id>` is stored in `./HuTao.Data/HuTao.Data.csproj`.
  * `Owner: ulong` - The User ID of the owner of the bot, this doesn't really do anything yet.
  * `Prefix: string` - The prefix that the bot will be using.
  * `Token: string` - The bot's token.
  * `HuTaoContext: string` - The postgres connection string for the bot's data itself, including but not limited to server configuration, users, logging, and moderation.
  * `HangfireContext: string` - The postgres connection string for the Hangfire library, this is used for tasks related to timing.
  * `MessageCacheSize: int` - The cache size for messages. Keep this to 100.

> `secrets.json`
> ```json
> {
>     "Debug": {
>       "Owner": 0,
>       "Guild": 0,
>       "GatewayIntents": 98047,
>       "Prefix": "h!",
>       "AlwaysDownloadUsers": true,
>       "Token": "ABCD.EFGH.IJKL",
>       "HuTaoContext": "Server=127.0.0.1;Port=5432;Database=HuTao;User Id=postgres;Password=[password];",
>       "HangfireContext": "Server=127.0.0.1;Port=5432;Database=Hangfire;User Id=postgres;Password=[password];",
>       "MessageCacheSize": 100
>     },
>     "Release": { }
> }
> ```

> You can get a bot token by [creating one yourself](https://discord.com/developers/applications) and use that token. A guide can be found [here](https://docs.stillu.cc/guides/getting_started/first-bot.html).

#### Database Setup
1. Install the dotnet-ef tools by running `dotnet tool install --global dotnet-ef`
2. Run `dotnet ef database update --project ./HuTao.Data/HuTao.Data.csproj` to create the HuTaoContext database.
3. Create an empty database with the same name that the `HangfireContext` uses.
  > You can use the command `CREATE DATABASE HangfireContextName` in the `psql.exe` program found in the installation of postgres. [Guide](https://www.microfocus.com/documentation/idol/IDOL_12_0/MediaServer/Guides/html/English/Content/Getting_Started/Configure/_TRN_Set_up_PostgreSQL.htm)

### Testing
There are no testing procedures yet, other than actually running the bot and testing it manually. In the future there should be projects that automate testing, aka TDD (Test Driven Development)

1. Set the project to Debug in the configuration, so that the user secrets will use the debug profile.
2. Run the bot and make sure that the new feature works as expected.
3. Ensure that no other features have regressed.

## Making Commits

### Commit name
The repository follows [conventional commits](https://www.conventionalcommits.org/en/v1.0.0) and uses the types `feat`, `refactor`, `chore`, and `docs`.
* Before committing, make sure you do a code cleanup using the `.editorconfig`.
* The commit name should follow conventional commit spec: `feat: Show roles that are modified in Role Management commands`
* The commit name should complete the sentence "This commit will..."
* Use the present tense, "Add featured" not "Added feature"
* Use the imperative mood, "Show roles..." not "Showing roles..."

### Branching
Commits should not be made directly on the `main` branch. The name of the branch should be prefixed with your github's username then a short description of what the branch is intended to do.

> `sabihoshi/role-management`

### Making a pull request
Tasks are broken down in the [HuTao Project](https://github.com/WFP-Doobelepers/HuTao/projects/1), if you are tackling a specific task, create a Pull Request targeting that specific issue. You can link the PR in two ways:
1. Link the Issue to the PR by vising the Issue and then under ["Linked pull requests"](https://docs.github.com/en/issues/tracking-your-work-with-issues/linking-a-pull-request-to-an-issue#manually-linking-a-pull-request-to-an-issue), link the PR that you just made.
2. Add the keywords in the PR's description according to the [GitHub docs](https://docs.github.com/en/issues/tracking-your-work-with-issues/linking-a-pull-request-to-an-issue).

## Tasks Explanation
1. `XL`: Tasks that are probably gonna be tackled by me, since they require deep knowledge about the back end and the direction of the entire bot.
2. `L`: Tasks  someone experienced in C# can work, they don't require a direction, but rather knowledge about refactoring or implementations of existing commands, to make them work better or faster.
3. `M`: These just require some knowledge about C#,  for example making a command that's fairly easy.
4. `S`: Just some things that can be considered chores, these are really easy that even with no knowledge of programming entirely can do.