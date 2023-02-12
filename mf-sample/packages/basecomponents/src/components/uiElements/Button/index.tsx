import React from "react"

type ButtonProps = {
} & JSX.IntrinsicElements["button"];

const Button: React.FC<ButtonProps> = ({ children, type }) => (
    <button type={type}>{children}</button>
);

export { Button }