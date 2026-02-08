use std::{
    sync::{
        Arc,
        atomic::{AtomicBool, Ordering},
    },
    thread,
    time::Duration,
};

use serial2::SerialPort;
use sollertia_core::Pressure;

fn main() {
    let running = Arc::new(AtomicBool::new(true));
    let r = running.clone();

    ctrlc::set_handler(move || {
        r.store(false, Ordering::SeqCst);
    })
    .expect("Error setting Ctrl-C handler");

    let port = SerialPort::open("/dev/cu.usbmodem1101", 9600).unwrap();

    let mut buffer = [0u8; 4];
    let mut is_started = false;
    let mut object: Result<Pressure, [u8; 4]>;

    thread::sleep(Duration::from_secs(2));

    port.write_all(&[83]).unwrap();
    port.flush().unwrap();

    while running.load(Ordering::SeqCst) {
        port.read_exact(&mut buffer).unwrap();
        object = Pressure::from_serial(buffer);
        // println!("{buffer:?} turns into {object:#?}");
        if let Ok(object) = object {
            if is_started {
                println!("{object}");
            } else if object.is_start() {
                is_started = true;
            }
        }
    }

    port.write_all(&[115]).unwrap();
    port.flush().unwrap();
}
