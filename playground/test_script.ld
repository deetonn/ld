
struct Data {
  data: u32
}

impl Data {
  fn get<T>() -> T {
    return 0
  }
}

fn get<T>() -> T {
  // trait defined generic functions have different syntax
  // to regular generic functions.
  return Data::get<T>::()
}

fn main() {
  let data = get::<bool>()
}