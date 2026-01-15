using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Fergun.Interactive.Extensions;
using Fergun.Interactive.Pagination;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Roles;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Roles;

public class RoleBrowserTests
{
    [Fact]
    public void RoleBrowserState_FilterByName_FiltersCorrectly()
    {
        var state = RoleBrowserState.Create("Test", CreateRoles());

        state.ApplyFilter("mod");

        var filtered = state.GetFilteredRoles();
        Assert.Single(filtered);
        Assert.Equal("Moderators", filtered[0].Name);
    }

    [Fact]
    public void RoleBrowserRenderer_GeneratePage_List_DoesNotThrow()
    {
        var (state, paginator) = Create(RoleBrowserView.List);

        var page = RoleBrowserRenderer.GeneratePage(paginator);

        Assert.NotNull(page);
        Assert.Equal(RoleBrowserView.List, state.View);
    }

    [Fact]
    public void RoleBrowserRenderer_GeneratePage_Detail_DoesNotThrow()
    {
        var roles = CreateRoles().ToList();
        var state = RoleBrowserState.Create("Test", roles);
        state.SelectRole(roles[0].Id);

        var paginator = CreatePaginator(state);

        var page = RoleBrowserRenderer.GeneratePage(paginator);

        Assert.NotNull(page);
        Assert.Equal(RoleBrowserView.Detail, state.View);
    }

    private static (RoleBrowserState State, Fergun.Interactive.Pagination.IComponentPaginator Paginator) Create(RoleBrowserView view)
    {
        var state = RoleBrowserState.Create("Test", CreateRoles());
        state.View = view;
        var paginator = CreatePaginator(state);
        return (state, paginator);
    }

    private static Fergun.Interactive.Pagination.IComponentPaginator CreatePaginator(RoleBrowserState state)
    {
        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(1);

        return InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(user.Object)
            .WithUserState(state)
            .WithPageCount(state.GetPageCount())
            .WithPageFactory(RoleBrowserRenderer.GeneratePage)
            .Build();
    }

    private static IReadOnlyCollection<RoleEntry> CreateRoles()
    {
        return new List<RoleEntry>
        {
            new(
                Id: 2,
                Name: "Moderators",
                Position: 10,
                Color: 0x00FF00,
                IsHoisted: true,
                IsMentionable: false,
                IsManaged: false,
                MemberCount: 3,
                PermissionsText: "KickMembers, BanMembers"),
            new(
                Id: 1,
                Name: "Everyone",
                Position: 0,
                Color: 0,
                IsHoisted: false,
                IsMentionable: false,
                IsManaged: true,
                MemberCount: 100,
                PermissionsText: "None")
        };
    }
}

