@tool
extends EditorPlugin

const HOST_BLOCK_BEGIN := "<!-- DotPudica:Begin -->"
const HOST_BLOCK_END := "<!-- DotPudica:End -->"
## Host project fragment kept in sync on every plugin load.
## Pure .NET unit tests and BenchmarkDotNet project stay excluded.
## Godot integration tests under tests/DotPudica.Integration must compile in the host.
const HOST_BLOCK := """<!-- DotPudica:Begin -->
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <DefaultItemExcludes>$(DefaultItemExcludes);tests/DotPudica.Tests/**;benchmarks/**</DefaultItemExcludes>
  </PropertyGroup>

  <ItemGroup>
    <Compile Remove="addons/dot-pudica/**/*.cs" />
    <Compile Remove="tests/DotPudica.Tests/**/*.cs" />
    <Compile Remove="benchmarks/**/*.cs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="addons/dot-pudica/Core/DotPudica.Core.csproj" />
    <ProjectReference Include="addons/dot-pudica/Godot/DotPudica.Godot.csproj" />
    <ProjectReference Include="addons/dot-pudica/SourceGenerator/DotPudica.SourceGenerator.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
<!-- DotPudica:End -->
"""

func _enter_tree() -> void:
    print("[DotPudica] Plugin enabled.")
    _ensure_unit_tests_gdignore()
    _ensure_benchmarks_gdignore()
    _update_host_project(true)


func _exit_tree() -> void:
    # Keep host block on editor restart / plugin reload so exclusions like
    # tests/DotPudica.Tests are never wiped and re-injected from an outdated template.
    # Removal only happens when the user disables/uninstalls via Project Settings.
    if not _is_plugin_still_enabled():
        _update_host_project(false)
        print("[DotPudica] Plugin disabled; host project block removed if present.")
    else:
        print("[DotPudica] Plugin reloading; host project block kept.")


func _is_plugin_still_enabled() -> bool:
    var enabled: PackedStringArray = ProjectSettings.get_setting("editor_plugins/enabled", PackedStringArray())
    var script := get_script() as Script
    if script == null:
        return false
    var plugin_path: String = script.resource_path.get_base_dir().path_join("plugin.cfg")
    for entry in enabled:
        if str(entry).replace("\\", "/") == plugin_path.replace("\\", "/"):
            return true
    return false


func _ensure_unit_tests_gdignore() -> void:
    # Legacy: whole tests/ was ignored when Integration still lived under samples/.
    # That would hide Godot integration tests — remove if present.
    var legacy_ignore := ProjectSettings.globalize_path("res://tests/.gdignore")
    if FileAccess.file_exists(legacy_ignore):
        DirAccess.remove_absolute(legacy_ignore)
        print("[DotPudica] Removed obsolete tests/.gdignore so Integration remains visible.")

    var unit_tests_dir := ProjectSettings.globalize_path("res://tests/DotPudica.Tests")
    if not DirAccess.dir_exists_absolute(unit_tests_dir):
        return

    var ignore_path := unit_tests_dir.path_join(".gdignore")
    if FileAccess.file_exists(ignore_path):
        return

    var file := FileAccess.open(ignore_path, FileAccess.WRITE)
    if file == null:
        push_warning("[DotPudica] Could not create tests/DotPudica.Tests/.gdignore")
        return

    file.store_string("# Ignore pure .NET unit tests from Godot import / C# UID generation.\n")
    file.close()
    print("[DotPudica] Created tests/DotPudica.Tests/.gdignore")


func _ensure_benchmarks_gdignore() -> void:
    var benchmarks_dir := ProjectSettings.globalize_path("res://benchmarks")
    if not DirAccess.dir_exists_absolute(benchmarks_dir):
        return

    var ignore_path := benchmarks_dir.path_join(".gdignore")
    if FileAccess.file_exists(ignore_path):
        return

    var file := FileAccess.open(ignore_path, FileAccess.WRITE)
    if file == null:
        push_warning("[DotPudica] Could not create benchmarks/.gdignore")
        return

    file.store_string("# Ignore BenchmarkDotNet project from Godot import / C# UID generation.\n")
    file.close()
    print("[DotPudica] Created benchmarks/.gdignore")


func _update_host_project(enable: bool) -> void:
    var csproj_path := _find_host_csproj()
    if csproj_path.is_empty():
        print("[DotPudica] No host .csproj found under res://, skipping project injection until the C# host project exists.")
        return

    var content := FileAccess.get_file_as_string(csproj_path)
    if content.is_empty():
        push_error("[DotPudica] Failed to read %s" % csproj_path)
        return

    var updated := content
    if enable:
        updated = _sync_host_block(content)
    else:
        updated = _remove_host_block(content)

    if updated == content:
        var state := "already synced" if enable else "already absent"
        print("[DotPudica] Host project configuration %s: %s" % [state, csproj_path])
        return

    var file := FileAccess.open(csproj_path, FileAccess.WRITE)
    if file == null:
        push_error("[DotPudica] Failed to open %s for writing" % csproj_path)
        return

    file.store_string(updated)
    file.close()

    var action := "Synced" if enable else "Removed"
    print("[DotPudica] %s host project configuration: %s" % [action, csproj_path])

func _find_host_csproj() -> String:
    var root := ProjectSettings.globalize_path("res://")
    var root_candidates := _find_csproj_files(root, false)
    for candidate in root_candidates:
        if not _is_plugin_csproj(candidate):
            return candidate

    var recursive_candidates := _find_csproj_files(root, true)
    for candidate in recursive_candidates:
        if not _is_plugin_csproj(candidate):
            return candidate

    return ""


func _find_csproj_files(path: String, recursive: bool) -> PackedStringArray:
    var results := PackedStringArray()
    var dir := DirAccess.open(path)
    if dir == null:
        return results

    dir.list_dir_begin()
    while true:
        var entry := dir.get_next()
        if entry.is_empty():
            break

        var entry_path := path.path_join(entry)
        if dir.current_is_dir():
            if recursive and not entry.begins_with("."):
                results.append_array(_find_csproj_files(entry_path, true))
            continue

        if entry.get_extension().to_lower() == "csproj":
            results.append(entry_path)
    dir.list_dir_end()

    return results


func _is_plugin_csproj(path: String) -> bool:
    var normalized := path.replace("\\", "/").to_lower()
    return normalized.contains("/addons/dot-pudica/")


func _sync_host_block(content: String) -> String:
    var begin_index := content.find(HOST_BLOCK_BEGIN)
    if begin_index == -1:
        return _inject_host_block(content)

    var end_index := content.find(HOST_BLOCK_END, begin_index)
    if end_index == -1:
        push_error("[DotPudica] Invalid injected block: missing end marker")
        return content

    end_index += HOST_BLOCK_END.length()
    while end_index < content.length() and (content[end_index] == "\n" or content[end_index] == "\r"):
        end_index += 1

    var existing := content.substr(begin_index, end_index - begin_index)
    var normalized_existing := existing.replace("\r\n", "\n").strip_edges()
    var normalized_expected := HOST_BLOCK.replace("\r\n", "\n").strip_edges()
    if normalized_existing == normalized_expected:
        return content

    var prefix := _trim_right_whitespace(content.substr(0, begin_index))
    var suffix := _trim_left_newlines(content.substr(end_index))
    if suffix.is_empty():
        return "%s\n\n%s\n" % [prefix, HOST_BLOCK]

    return "%s\n\n%s\n%s" % [prefix, HOST_BLOCK, suffix]


func _inject_host_block(content: String) -> String:
    var project_close := "</Project>"
    var close_index := content.rfind(project_close)
    if close_index == -1:
        push_error("[DotPudica] Invalid .csproj format: missing </Project>")
        return content

    var prefix := _trim_right_whitespace(content.substr(0, close_index))
    return "%s\n\n%s%s" % [prefix, HOST_BLOCK, project_close]

func _remove_host_block(content: String) -> String:
    var begin_index := content.find(HOST_BLOCK_BEGIN)
    if begin_index == -1:
        return content

    var end_index := content.find(HOST_BLOCK_END, begin_index)
    if end_index == -1:
        push_error("[DotPudica] Invalid injected block: missing end marker")
        return content

    end_index += HOST_BLOCK_END.length()
    if end_index < content.length() and content[end_index] == "\n":
        end_index += 1
    if end_index < content.length() and content[end_index] == "\r":
        end_index += 1

    var prefix := _trim_right_whitespace(content.substr(0, begin_index))
    var suffix := _trim_left_newlines(content.substr(end_index))
    if suffix.is_empty():
        return "%s\n" % prefix

    return "%s\n\n%s" % [prefix, suffix]


func _trim_right_whitespace(text: String) -> String:
    var end_index := text.length() - 1
    while end_index >= 0:
        var ch := text[end_index]
        if ch != " " and ch != "\t" and ch != "\r" and ch != "\n":
            break
        end_index -= 1

    if end_index < 0:
        return ""

    return text.substr(0, end_index + 1)


func _trim_left_newlines(text: String) -> String:
    var start_index := 0
    while start_index < text.length():
        var ch := text[start_index]
        if ch != "\r" and ch != "\n":
            break
        start_index += 1

    return text.substr(start_index)
