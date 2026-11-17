# SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

## Script errors

ntsl-error-at-line = Line {$line}: {$error}
ntsl-error-unclosed-comment = This /* comment is never closed with */.
ntsl-error-unclosed-string = This text is missing its closing quote.
ntsl-error-unexpected-character = '{$character}' can't be used here.
ntsl-error-unexpected-end = The code ended too early. Is a {"}"} or ; missing?
ntsl-error-expected-symbol = Expected '{$symbol}' but found '{$found}'.
ntsl-error-expected-name = Expected a name but found '{$found}'.
ntsl-error-expected-expression = Expected a value but found '{$found}'.
ntsl-error-invalid-assignment = Only variables, list items and fields can be assigned to.
ntsl-error-too-deep = The code is nested too deeply...
ntsl-error-code-too-long = Sorry! Scripts can be at most {$max} characters long.
ntsl-error-too-many-statements = The script ran more than {$max} statements and was stopped.
ntsl-error-too-many-loops = A loop ran more than {$max} times and was stopped.
ntsl-error-too-much-recursion = Functions called each other more than {$max} levels deep.
ntsl-error-too-slow = The script took too long and was stopped.
ntsl-error-unknown-name = '{$name}' doesn't exist.
ntsl-error-not-a-function = Only functions can be called.
ntsl-error-not-a-number = Expected a number.
ntsl-error-not-a-list = Expected a list.
ntsl-error-not-a-signal = broadcast() needs a signal, made with signal().
ntsl-error-divide-by-zero = Can't divide by zero.
ntsl-error-no-member = This value has no field '{$name}'.
ntsl-error-bad-index = This value can't be indexed like that.
ntsl-error-bad-key = Keys must be text.
ntsl-error-index-out-of-range = Index {$index} is outside the list (it has {$count} items).
ntsl-error-list-too-long = Lists can hold at most {$max} items.
ntsl-error-too-many-broadcasts = A message can send at most {$max} broadcasts.
ntsl-error-upload-cooldown = Wait a moment before uploading again.

## Traffic control console

ntsl-console-window-title = Traffic control console
ntsl-console-window-server = Server:
ntsl-console-window-choose-server = Choose a server...
ntsl-console-window-refresh = Refresh
ntsl-console-window-no-server = Choose a telecommunication server to edit its script.
ntsl-console-window-enabled = Run scripts
ntsl-console-window-upload = Compile and upload
ntsl-console-window-clear = Clear
ntsl-console-window-restore = Restore previous
ntsl-console-window-restore-tooltip = Puts back the script this server had before the last upload.
ntsl-console-window-log = Errors while running:
ntsl-console-window-log-empty = No errors.
ntsl-console-window-placeholder = def process_signal(sig) {"{"} return sig; {"}"}
ntsl-console-window-hint-tip = Tab finishes a name you started typing.
ntsl-console-window-hint-choices = Tab: {$names}
ntsl-console-server-label = {$name} #{$id} ({$channels}){$scripted ->
    [true] {" "}[script]
   *[false] {""}
}
ntsl-console-server-no-keys = no keys
ntsl-console-upload-success = Script compiled and uploaded.
ntsl-console-cleared = Script cleared.
ntsl-console-restored = Previous script restored.

## Admin commands

cmd-ntsllist-desc = Lists every telecom server that has an NTSL script.
cmd-ntsllist-help = Usage: ntsllist
cmd-ntsllist-entry = {$server} | running: {$enabled} | {$length} characters | last edited by {$editor}
cmd-ntsllist-none = No telecom server has an NTSL script.
cmd-ntslshow-desc = Prints a telecom server's NTSL script and its recent errors.
cmd-ntslshow-help = Usage: ntslshow <server entity id>
cmd-ntslwipe-desc = Deletes the NTSL script from one telecom server, or from all of them.
cmd-ntslwipe-help = Usage: ntslwipe <server entity id | all>
cmd-ntslwipe-all = Wiped {$count} NTSL scripts.
cmd-ntslwipe-one = Wiped the NTSL script on {$server}.
cmd-ntsl-no-script = {$server} is not a telecom server with an NTSL script.

## Station events

station-event-ntsl-rogue-script-announcement = Unknown code has been detected on one of the station's telecommunication servers.
station-event-ntsl-centcom-filter-start-announcement = Central Command has installed a mandatory communications compliance script on all telecommunication servers. It will be lifted automatically. Please do not remove it.
station-event-ntsl-centcom-filter-removed-announcement = Central Command has detected that its communications compliance script was removed ahead of schedule. This has been noted in the station's quarterly performance review.
station-event-ntsl-centcom-filter-end-announcement = The communications compliance script has been lifted. Previous server scripts have been restored.

## Guidebook

guide-entry-ntsl = NTSL Scripting
