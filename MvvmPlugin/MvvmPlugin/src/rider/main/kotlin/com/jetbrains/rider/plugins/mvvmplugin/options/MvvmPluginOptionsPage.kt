package com.jetbrains.rider.plugins.mvvmplugin.options

import com.jetbrains.rider.plugins.mvvmplugin.OptionPagesBundle
import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class MvvmPluginOptionsPage : SimpleOptionsPage(
    name = OptionPagesBundle.message("configurable.name.optionpages.options.title"),
    // Must match the OptionsPage PID in the .NET MvvmPluginOptionsPage.
    pageId = "MvvmPluginOptionsPage"
) {
    override fun getId(): String = "MvvmPluginOptionsPage"
}
