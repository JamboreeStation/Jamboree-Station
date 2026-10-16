# SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

custom-radio-error-too-many = There are too many custom channels this round.
custom-radio-error-unknown = That channel no longer exists.
custom-radio-error-name = Names must be 1-24 letters, numbers, spaces, ' or -.
custom-radio-error-name-taken = Another channel already uses that name.
custom-radio-error-key = Prefixes must be 2-6 letters (a-z).
custom-radio-error-key-taken = Another channel already uses that prefix.
custom-radio-error-color = That channel color is too dark.

custom-key-window-title = Set up encryption key
custom-key-window-name = Channel name
custom-key-window-prefix = Prefix (type :#prefix)
custom-key-window-color = Chat colour
custom-key-window-confirm = Set up
custom-key-already-set-up = This key is already set up. Edit its channel at a telecom server.
custom-key-set-up = Key set up for {$name}. Talk on it with :#{$key}

custom-key-copied = You copy the channel configuration onto the blank key.
custom-key-copy-not-blank = You can only copy onto a blank key.

custom-key-name = {$name} encryption key
custom-key-desc = An encryption key for a custom radio channel.
custom-key-window-preview = [{$name}] 1... 2... 3... Here's how this looks like!

custom-radio-server-window-title = Custom radio channels
custom-radio-server-window-empty = No custom channel keys are installed in this server.
custom-radio-server-window-edit = Edit
custom-radio-server-window-save = Save
custom-radio-server-saved = Channel updated.
custom-radio-server-window-row = {$name} (:#{$key}) - {$count ->
    [one] 1 headset
    *[other] {$count} headsets
}
custom-radio-server-examine = [color={$color}]{$name} (:#{$key})[/color] - {$count ->
    [one] 1 headset connected
    *[other] {$count} headsets connected
}