
fn identifier(data: u32) -> u32 {
  return 2
}

struct ParseError {
  message: String,
  code: u64
}

struct Node {
  expression: Box<Expression>
}

enum ParseResult {
  AstNode(Node),
  Failure(ParseError)
}

let result = ParseResult::Failure(ParseError {
  message: "Failure!",
  code: 0
})

fn main() {
  if result == identifier() {
    println("ok")
  }
}