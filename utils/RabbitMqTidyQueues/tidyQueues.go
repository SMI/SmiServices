package main

import (
	"encoding/json"
	"io/ioutil"
	"net/http"
	"net/url"
	"strconv"
	"strings"
)

func main() {

	println("Disposing unused control queues...")

	client := &http.Client{}

	req, err := http.NewRequest("GET", "http://127.0.0.1:15672/api/queues/", nil)
	FatalIf(err)

	req.SetBasicAuth("guest", "guest")

	resp, err := client.Do(req)
	FatalIf(err)

	type Queue struct {
		Name  string `json:"name"`
		VHost string `json:"vhost"`
	}

	queues := make([]Queue, 0)
	json.NewDecoder(resp.Body).Decode(&queues)

	req.Method = "DELETE"

	for _, q := range queues {

		if strings.HasPrefix(q.Name, "Control.") {

			if q.VHost == "/" {
				q.VHost = "%2f"
			}

			api := "http://127.0.0.1:15672/api/queues/" + q.VHost + "/" + q.Name
			params := "?if-unused=true"

			parsed, err := url.Parse(api + params)
			FatalIf(err)

			req.URL = parsed

			print(req.Method + " " + req.URL.String() + " -> ")

			resp, err := client.Do(req)
			FatalIf(err)

			if resp.StatusCode == http.StatusNoContent {
				println("Deleted")
				continue
			}

			bodyBytes, err := ioutil.ReadAll(resp.Body)
			FatalIf(err)

			bodyStr := string(bodyBytes)

			if strings.Contains(bodyStr, "in use") {

				println("In use")

			} else {

				println(":: " + strconv.Itoa(resp.StatusCode) + ", " + bodyStr)
				panic(bodyStr)
			}
		}
	}
}

func FatalIf(err error) {
	if err != nil {
		panic(err)
	}
}
