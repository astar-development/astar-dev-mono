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
- `ScrollViewer` MUST be in a `*` Grid row — never `Auto` or inside `StackPanel`.
- `MinHeight="0"` REQUIRED — Avalonia's default minimum breaks star-row collapse.
- Nav-host `ContentControl` MUST set `HorizontalContentAlignment="Stretch"` `VerticalContentAlignment="Stretch"` and live in a `*` row.

## UserControl Root

Every navigation-target `UserControl` must declare `VerticalContentAlignment="Stretch" HorizontalContentAlignment="Stretch"`.
