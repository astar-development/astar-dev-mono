UI layout and interaction patterns for Avalonia AXAML in this repository.

## Scrollable Views — Bounded Viewport Rule

**Root cause of clipping/no-scroll bugs**: A `ScrollViewer` only scrolls when its measured height is bounded. Any parent that sizes itself to content (`StackPanel`, `Auto` Grid row, unsized `ContentControl`) gives the `ScrollViewer` unlimited height — it expands to fit and never scrolls. Content at the bottom is clipped instead of reachable.

### Anti-pattern

```xml
<!-- ❌ StackPanel gives unlimited height → ScrollViewer never scrolls -->
<StackPanel>
    <ScrollViewer>
        ...growing content...
    </ScrollViewer>
</StackPanel>

<!-- ❌ ContentControl with Auto sizing → same problem one level up -->
<ContentControl Content="{Binding CurrentView}"/>
```

### Correct pattern

```xml
<!-- ✅ Grid star row bounds the ScrollViewer → scrolls correctly -->
<Grid RowDefinitions="Auto,*">
    <StackPanel Grid.Row="0" ...>
        <!-- fixed header / toolbar -->
    </StackPanel>

    <ScrollViewer Grid.Row="1"
                  VerticalScrollBarVisibility="Auto"
                  HorizontalScrollBarVisibility="Disabled"
                  MinHeight="0">
        <StackPanel ...>
            <!-- growing content -->
        </StackPanel>
    </ScrollViewer>
</Grid>
```

### Rules

- **Always** put a `ScrollViewer` in a `*`-sized `Grid` row, never in an `Auto` row or directly inside a `StackPanel`.
- **Always** set `MinHeight="0"` on the `ScrollViewer` — Avalonia's default minimum can prevent the star row from collapsing it correctly.
- **Shell / nav hosts**: any `ContentControl` that hosts routed views MUST set `HorizontalContentAlignment="Stretch"` and `VerticalContentAlignment="Stretch"`, and live in a `*` row itself — otherwise the viewport bound is lost one level up and the inner `ScrollViewer` still never scrolls.
- When a view grows beyond a fixed area (lists, form rows, classification rules, accounts), **always** use this `Grid RowDefinitions="Auto,*"` + `ScrollViewer` structure from the start.
- Prefer a dedicated view (`UserControl`) for content that can grow unboundedly (e.g. classification rules, account lists) rather than embedding it in a parent settings page — this keeps the viewport ownership unambiguous.

## UserControl Root

Every `UserControl` that is navigation-target must declare:

```xml
<UserControl ...
             VerticalContentAlignment="Stretch"
             HorizontalContentAlignment="Stretch">
```

This ensures the shell's `ContentControl` can stretch it to fill the available area.
