## ScrollViewer — Bounded Viewport Rule

`ScrollViewer` only scrolls when its measured height is bounded. `StackPanel`, `Auto` rows, and unsized `ContentControl` give unlimited height — content clips instead of scrolling.

```xml
<!-- ❌ never scrolls — ScrollViewer direct child of UserControl -->
<UserControl VerticalContentAlignment="Stretch">
    <ScrollViewer>...</ScrollViewer>
</UserControl>

<!-- ❌ never scrolls — lone * row cannot bind height when Grid is measured with infinite space -->
<Grid RowDefinitions="*">
    <ScrollViewer Grid.Row="0">...</ScrollViewer>
</Grid>

<!-- ✅ correct — real content in Auto row pins the * row height -->
<UserControl VerticalContentAlignment="Stretch"
             HorizontalContentAlignment="Stretch">
    <Grid RowDefinitions="Auto,*"
          HorizontalAlignment="Stretch"
          VerticalAlignment="Stretch">

        <StackPanel Grid.Row="0" Margin="24,20,24,12" Spacing="4">
            <TextBlock Text="Page title" FontSize="{StaticResource FontSizeXl}" FontWeight="Medium"/>
            <TextBlock Text="Page description." FontSize="{StaticResource FontSizeSm}"/>
        </StackPanel>

        <ScrollViewer Grid.Row="1"
                      VerticalScrollBarVisibility="Auto"
                      HorizontalScrollBarVisibility="Disabled"
                      MinHeight="0"
                      Padding="24,0,24,20">
            <StackPanel/>
        </ScrollViewer>

    </Grid>
</UserControl>
```

Rules:
- `ScrollViewer` MUST be in the `*` row of a `Grid RowDefinitions="Auto,*"` — this is the **only** reliably bounded pattern.
- The `Grid` MUST declare `HorizontalAlignment="Stretch" VerticalAlignment="Stretch"`.
- The `Auto` row MUST contain real content (page title, header strip, toolbar). Empty `Auto` rows are forbidden — use a meaningful UI element.
- **NEVER** place `ScrollViewer` as a direct child of `UserControl` even with `VerticalContentAlignment="Stretch"` — `VerticalContentAlignment` affects rendering/positioning only, NOT measurement. ContentPresenter passes infinite height to its child during measure regardless of VCA, so the ScrollViewer's Extent equals its Viewport and scrolling never triggers.
- **NEVER** wrap a `ScrollViewer` in a single-star-row `<Grid RowDefinitions="*">` — a lone `*` row cannot bind viewport height when the Grid is measured with infinite space.
- `MinHeight="0"` REQUIRED on the `ScrollViewer` — Avalonia's default minimum breaks star-row collapse.
- Nav-host `ContentControl` MUST set `HorizontalContentAlignment="Stretch"` `VerticalContentAlignment="Stretch"` and live in a `*` row.

## UserControl Root

Every navigation-target `UserControl` must declare `VerticalContentAlignment="Stretch" HorizontalContentAlignment="Stretch"`.
