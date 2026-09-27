package com.jetbrains.rider.plugins.mvvmplugin

import com.intellij.DynamicBundle
import org.jetbrains.annotations.Nls
import org.jetbrains.annotations.NonNls
import org.jetbrains.annotations.PropertyKey

class OptionPagesBundle {
    companion object {
        @NonNls
        private const val BUNDLE = "messages.OptionPagesBundle"
        private val INSTANCE = DynamicBundle(OptionPagesBundle::class.java, BUNDLE)

        @Nls
        fun message(
            @PropertyKey(resourceBundle = BUNDLE) key: String,
            vararg params: Any
        ): String {
            return INSTANCE.getMessage(key, *params)
        }
    }
}
