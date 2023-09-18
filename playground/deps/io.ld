
struct IoError {}

/*
  The basic IO result. This always returns IoError on Err variant.
  This allows for flawless I/O code propogating errors.
*/
enum Result<T> {
  Ok(T), Err(IoError)
}

fn open_file(path: String) -> Result<String> {
  return Result::Ok("the file contents")
}