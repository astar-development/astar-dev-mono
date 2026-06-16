## ScrollViewer — Bounded Viewport Rule

`ScrollViewer` only scrolls when its measured height is bounded. `StackPanel`, `Auto` rows, and unsized `ContentControl` give unlimited height — content clips instead of scrolling.

```xml
<!-- ❌ never scrolls -->
<StackPanel><ScrollViewer>...</ScrollViewer></StackPanel>

<!-- ✅ correct -->
<Grid RowDefinitions="Auto,*">
    <StackPanel Grid.Row="0"/>
    <ScrollViewer Grid.Row="1"
                  VerticalScrollBarVisibility="Auto"
                  HorizontalScrollBarVisibility="Disabled"
                  MinHeight="0">
        <StackPanel/>
    </ScrollViewer>
</Grid>
```

Rules:
- `ScrollViewer` MUST be in a `*` row of a `Grid RowDefinitions="Auto,*"` — this is the **only** reliably bounded pattern.
- **NEVER** place `ScrollViewer` as a direct child of `UserControl` even with `VerticalContentAlignment="Stretch"` — `VerticalContentAlignment` affects rendering/positioning only, NOT measurement; ContentPresenter still passes infinite height to its child during measure, so the ScrollViewer's Extent equals its Viewport and scrolling never triggers.
- **NEVER wrap a `ScrollViewer` in a single-star-row `<Grid RowDefinitions="*">`** — a lone `*` row cannot bind viewport height when the Grid is measured with infinite space.
- `MinHeight="0"` REQUIRED — Avalonia's default minimum breaks star-row collapse.
- The `Auto` row MUST contain real content (a header strip, title bar, toolbar). A zero-height `Auto` row also works structurally but prefer real content.
- Nav-host `ContentControl` MUST set `HorizontalContentAlignment="Stretch"` `VerticalContentAlignment="Stretch"` and live in a `*` row.

## UserControl Root

Every navigation-target `UserControl` must declare `VerticalContentAlignment="Stretch" HorizontalContentAlignment="Stretch"`.
