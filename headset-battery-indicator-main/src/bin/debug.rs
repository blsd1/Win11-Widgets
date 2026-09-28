use headset_battery_indicator::run;
use log::error;
use simplelog::{ConfigBuilder, TermLogger};

fn main() {
    TermLogger::init(
        log::LevelFilter::Trace,
        ConfigBuilder::new()
            .add_filter_ignore_str("ureq")
            .add_filter_ignore_str("rustls")
            .build(),
        simplelog::TerminalMode::Mixed,
        simplelog::ColorChoice::Auto,
    )
    .unwrap();

    if let Err(e) = run() {
        error!("Application stopped unexpectedly: {e:?}");
    }
}
