
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions;

/*
 * Use statements will work like this:
 * This example will use the following as 
 * it's main point: "use std::io::File"
 * 
 * The way a use statements works is,
 * the "std::io" part is simply a different
 * format for a path. So it could be
 * translated down to "use std/io.ld".
 * 
 * Whatever is interpreting this must treat them
 * in this way. There will be a base path that
 * "use" statements point to. So, for example,
 * "src/packages" could be the base. This directory
 * could include the standard by default and any
 * other packages the developer installs for the
 * current project gets put into here.
 * 
 * So then if the developer install the package
 * "json" they can easily say "use json::*"
 * 
 * The syntax will be similar to rust, except
 * nesting other modules within a use statement
 * will be disallowed. This is because it looks
 * to verbose and becomes really hard to read.
 * 
 * "use std::io::*" <- should import the entire
 *                     "io" module.
 * 
 * The star syntax signifies that you want to import
 * everything from that module.
 * 
 * "use std::mod_name::{struct1, funcN, enumN, ..., XN}
 * 
 * This should import what the user wants. If the
 * declarations being imported require another type,
 * for example it's a function that takes a non-imported
 * type as an argument, "non-imported type" should be automatically
 * imported too.
 * 
 * "use std::io"
 * 
 * This should be a shorthand for import all.
 * In other words, this directly translates to
 * example NO.1
 * 
 * "use" statements are similar to #include
 * in the way that the selected things
 * are just brung into the current AST.
*/
