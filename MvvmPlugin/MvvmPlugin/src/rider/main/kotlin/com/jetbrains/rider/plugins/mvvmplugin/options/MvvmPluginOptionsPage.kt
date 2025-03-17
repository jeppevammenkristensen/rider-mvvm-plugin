package com.jetbrains.rider.plugins.mvvmplugin.options

import com.jetbrains.rider.plugins.mvvmplugin.OptionPagesBundle
import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class MvvmPluginOptionsPage : SimpleOptionsPage(
    name = OptionPagesBundle.message("configurable.name.optionpages.options.title"), // this is defined in resources\messages\OptionPagesBundle.properties
    pageId = "MvvmPluginOptionsPage" // Must be in sync with SamplePage.PID
) {
    override fun getId(): String {
        return "MvvmPluginOptions"
    }
}