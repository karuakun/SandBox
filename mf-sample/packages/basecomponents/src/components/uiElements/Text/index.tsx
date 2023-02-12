import React from "react"

type TextProps = {
} & JSX.IntrinsicElements["span"];

const Text: React.FC<TextProps> = ({ children }) => (
    <span>{children}</span>
);

export { Text }