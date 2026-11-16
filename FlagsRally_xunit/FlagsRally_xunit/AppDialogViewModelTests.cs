using FlagsRally.ViewModels;

namespace FlagsRallyTests.ViewModels;

public class AppDialogViewModelTests
{
    [Fact]
    public async Task A_question_is_accepted_only_by_its_filled_button()
    {
        var accepted = new MessagePopupViewModel("Confirmation", "Delete?", "Yes", "No");
        await accepted.AcceptCommand.ExecuteAsync(null);
        Assert.True(accepted.Accepted);

        var refused = new MessagePopupViewModel("Confirmation", "Delete?", "Yes", "No");
        await refused.CancelCommand.ExecuteAsync(null);
        Assert.False(refused.Accepted);

        // Closed by a tap outside: no command runs
        Assert.False(new MessagePopupViewModel("Confirmation", "Delete?", "Yes", "No").Accepted);
    }

    [Fact]
    public void A_notice_has_one_button_and_a_question_two()
    {
        var notice = new MessagePopupViewModel("Completed", "Saved.", "OK");
        Assert.True(notice.IsNotice);
        Assert.False(notice.IsQuestion);

        var question = new MessagePopupViewModel("Confirmation", "Delete?", "Yes", "No");
        Assert.True(question.IsQuestion);
        Assert.False(question.IsNotice);
    }

    [Fact]
    public void A_message_of_blank_lines_takes_no_room()
    {
        Assert.False(new MessagePopupViewModel("Confirmation", "\n\n", "Yes", "No").HasMessage);
        Assert.True(new MessagePopupViewModel("Confirmation", "Delete?", "Yes", "No").HasMessage);
    }

    [Fact]
    public async Task Picking_one_gives_its_index_and_ticks_only_the_one_in_use()
    {
        var viewModel = ChoicePopupViewModel.PickOne("Sort by", ["Default", "Name", "Visited"], current: 1);

        Assert.True(viewModel.IsPickOne);
        Assert.Equal([false, true, false], viewModel.Rows.Select(row => row.IsTicked));
        Assert.Null(viewModel.PickedIndex);

        await viewModel.TapRowCommand.ExecuteAsync(viewModel.Rows[2]);

        Assert.Equal(2, viewModel.PickedIndex);
    }

    [Fact]
    public async Task Closing_without_picking_gives_nothing()
    {
        var viewModel = ChoicePopupViewModel.PickOne("Sort by", ["Default", "Name"]);

        await viewModel.CloseCommand.ExecuteAsync(null);

        Assert.Null(viewModel.PickedIndex);
        Assert.DoesNotContain(viewModel.Rows, row => row.IsTicked);
    }

    [Fact]
    public async Task An_option_switches_at_once_each_time_it_is_tapped()
    {
        var switches = new List<(int, bool)>();
        var viewModel = ChoicePopupViewModel.Options(
            "Display options", [("No duplicates", false), ("Hide date", true)], (index, isOn) => switches.Add((index, isOn)));

        Assert.True(viewModel.IsOptions);
        await viewModel.TapRowCommand.ExecuteAsync(viewModel.Rows[1]);
        await viewModel.TapRowCommand.ExecuteAsync(viewModel.Rows[0]);
        await viewModel.TapRowCommand.ExecuteAsync(viewModel.Rows[1]);

        Assert.Equal([(1, false), (0, true), (1, true)], switches);
        Assert.Equal([true, true], viewModel.Rows.Select(row => row.IsTicked));
        Assert.Null(viewModel.PickedIndex);
    }

    [Fact]
    public void A_tick_shows_on_the_row_in_use_and_a_box_on_every_option()
    {
        var pick = ChoicePopupViewModel.PickOne("Sort by", ["Default", "Name"], current: 0);
        Assert.Equal([true, false], pick.Rows.Select(row => row.ShowsTick));
        Assert.DoesNotContain(pick.Rows, row => row.IsOption);

        var options = ChoicePopupViewModel.Options("Display options", [("Hide date", true)], (_, _) => { });
        Assert.All(options.Rows, row => Assert.True(row.IsOption));
        Assert.DoesNotContain(options.Rows, row => row.ShowsTick);
    }

    [Fact]
    public void Only_a_list_longer_than_a_screenful_is_made_as_it_scrolls()
    {
        string[] Labels(int count) => [.. Enumerable.Range(0, count).Select(i => $"Row {i}")];

        Assert.True(ChoicePopupViewModel.PickOne("Countries", Labels(ChoicePopupViewModel.LongListRows)).IsShort);
        Assert.True(ChoicePopupViewModel.PickOne("Countries", Labels(ChoicePopupViewModel.LongListRows + 1)).IsLong);
    }

    [Fact]
    public void Every_row_but_the_last_has_a_line_under_it()
    {
        var viewModel = ChoicePopupViewModel.PickOne("Sort by", ["Default", "Name", "Visited"]);
        Assert.Equal([true, true, false], viewModel.Rows.Select(row => row.HasDivider));
    }
}
