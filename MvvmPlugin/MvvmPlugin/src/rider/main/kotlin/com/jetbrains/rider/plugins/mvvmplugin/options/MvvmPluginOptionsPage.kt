package com.jetbrains.rider.plugins.mvvmplugin.options

import com.jetbrains.rider.plugins.mvvmplugin.OptionPagesBundle
import com.intellij.openapi.options.BoundConfigurable
import com.intellij.ui.dsl.builder.panel

class MvvmPluginOptionsPage : BoundConfigurable(
    OptionPagesBundle.message("configurable.name.optionpages.options.title")
) {
    override fun createPanel() = panel {}
}
