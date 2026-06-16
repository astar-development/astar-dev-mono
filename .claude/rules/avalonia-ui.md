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
- `ScrollViewer` MUST be in a `*` Grid row, OR the direct child of a `UserControl` with `VerticalContentAlignment="Stretch"` — never `Auto` or inside `StackPanel`.
- **NEVER wrap a `ScrollViewer` in a single-star-row `<Grid RowDefinitions="*">`** — a lone `*` row cannot bind viewport height when the Grid is measured with infinite space. Use `Auto,*` (with real Auto content) or remove the Grid.
- `MinHeight="0"` REQUIRED — Avalonia's default minimum breaks star-row collapse.
- Nav-host `ContentControl` MUST set `HorizontalContentAlignment="Stretch"` `VerticalContentAlignment="Stretch"` and live in a `*` row.

## UserControl Root

Every navigation-target `UserControl` must declare `VerticalContentAlignment="Stretch" HorizontalContentAlignment="Stretch"`.
