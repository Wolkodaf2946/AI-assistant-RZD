import matplotlib.pyplot as plt
from datetime import datetime, timedelta
import random
from tqdm import tqdm

plt.style.use('dark_background')
for graph_num in tqdm(range(10)):
    def generate_train_names(count):
        rus_letters = 'АБВГДЕЖЗИКЛМНОПРСТУФХЦЧШЩЭЮЯ'
        used_names = set()
        names = []

        while len(names) < count:
            letter = random.choice(rus_letters)
            number = random.randint(1, 999)
            name = f"{letter}{number:03d}"

            if name not in used_names:
                used_names.add(name)
                names.append(name)

        return names

    num_trains = 8
    stations = [f"Станция {chr(65+i)}" for i in range(10)]
    station_distances = [random.randint(15, 40) for _ in range(len(stations)-1)]

    train_names = generate_train_names(num_trains)

    train_params = {
        "avg_speed": 60,
        "speed_variation": 15,
        "min_stop_time": 2,
        "max_stop_time": 5
    }

    train_colors = ['#FF0000', '#00FF00', '#0000FF', '#FF00FF', '#00FFFF', '#FFFF00']

    start_time = datetime(2023, 1, 1, 11, 0)
    cutoff_time = datetime(2023, 1, 1, 19, 0)
    end_time = cutoff_time + timedelta(hours=2)

    def calculate_travel_time(distance_km, avg_speed, variation):
        speed = avg_speed + random.uniform(-variation, variation)
        return (distance_km / max(30, min(120, speed))) * 60

    trains_data = []
    for train_id in range(num_trains):
        color = train_colors[train_id % len(train_colors)]
        direction = random.choice([1, -1])
        start_station = random.randint(0, len(stations)-1)

        if direction == 1:
            end_station = len(stations) - 1
            distances = station_distances[start_station:]
        else:
            end_station = 0
            distances = station_distances[:start_station][::-1]

        departure_time = start_time + timedelta(minutes=random.randint(0, 120))
        current_time = departure_time
        station_times = []
        current_station = start_station

        for distance in distances:
            travel_min = calculate_travel_time(distance, train_params["avg_speed"], train_params["speed_variation"])
            arrival_time = current_time + timedelta(minutes=travel_min)

            if arrival_time > cutoff_time:
                station_times.append((current_station, cutoff_time, 0))
                break

            stop_time = random.randint(train_params["min_stop_time"], train_params["max_stop_time"])
            departure_next = arrival_time + timedelta(minutes=stop_time)

            if departure_next > cutoff_time:
                station_times.append((current_station, arrival_time, (cutoff_time - arrival_time).total_seconds()/60))
                break

            station_times.append((current_station, arrival_time, stop_time))
            current_time = departure_next
            current_station += direction

        trains_data.append({
            "id": train_names[train_id],
            "direction": direction,
            "start_station": start_station,
            "departure": departure_time,
            "station_times": station_times,
            "color": color,
        })

    fig, ax = plt.subplots(figsize=(18, 10))
    fig.patch.set_facecolor('#121212')
    ax.set_facecolor('#1E1E1E')

    for train in trains_data:
        x_values = [train["departure"]] + [time for _, time, _ in train["station_times"]]
        y_values = [train["start_station"]] + [station for station, _, _ in train["station_times"]]

        line = ax.plot(
            x_values,
            y_values,
            marker='o',
            markersize=8,
            linewidth=3,
            color=train["color"],
            label=f"{train['id']} ({'→' if train['direction']==1 else '←'} {stations[train['start_station']]})",
            markerfacecolor=train["color"],
            markeredgecolor='white',
            markeredgewidth=1.5
        )[0]

        ax.text(
            x_values[len(x_values)//2], y_values[len(x_values)//2] + 0.1,
            train['id'],
            color='white',
            fontsize=9,
            ha='center',
            va='bottom',
            bbox=dict(facecolor=train["color"], alpha=0.7, boxstyle='round,pad=0.2')
        )
    ax.set_yticks(range(len(stations)))
    ax.set_yticklabels(stations, fontsize=12, color='white')
    ax.set_xlim([start_time, end_time])
    ax.set_ylim([-0.5, len(stations)-0.5])

    ax.set_xlabel("Время", fontsize=14, labelpad=15, color='white')
    ax.set_ylabel("Станции", fontsize=14, labelpad=15, color='white')
    ax.set_title("График движения поездов", fontsize=16, pad=20, color='white')


    ax.grid(True, linestyle='--', color='#666666', alpha=0.4)

    fig.autofmt_xdate()
    plt.tight_layout()
    plt.savefig(f'graphics_test/graphic_{graph_num}.png')
    plt.close(fig)
    # plt.show()
