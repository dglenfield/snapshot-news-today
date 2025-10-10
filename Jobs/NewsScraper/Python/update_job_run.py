import sqlite3
import argparse

def update_job_run(db_path, job_id, new_status):
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    cursor.execute(
        "UPDATE scrape_news_job_run SET status = ? WHERE id = ?",
        (new_status, job_id)
    )
    conn.commit()
    conn.close()

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--db-path', type=str, required=True, help='Path to SQLite database file')
    parser.add_argument('--id', type=int, required=True, help='ID of the job run to update')
    parser.add_argument('--status', type=str, required=True, help='New status value')
    args = parser.parse_args()

    update_job_run(args.db_path, args.id, args.status)

if __name__ == "__main__":
    main()